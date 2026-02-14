#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import argparse
from dataclasses import dataclass
from datetime import datetime, timedelta

import numpy as np
import pandas as pd

from data_source import get_kline
from dynamic_scorer import DynamicScorer
from score_formula_parser import ScoreFormulaParser
from abtest_full_engine import optimize_ab_full_walkforward
from backtest_engine import BacktestParams, run_backtest


DEFAULT_FORMULA_TEXT = """评分公式设计
趋势强度（30分）：
- MA5 > MA20（10分）
- MA10 > MA30（10分）
- 均线多头排列（MA5>MA10>MA20）（5分）
- 近期5日涨幅 > 3%（5分）
动量确认（25分）：
- MACD金叉且柱状图扩大（10分）
- KDJ（K>D且在20-80区间）（10分）
- 收盘价突破布林带中轨且带宽扩张（5分）
量价配合（20分）：
- 成交量 > 20日均量1.3倍（10分）
- 量比（当日/5日均量）>1.2且持续2天（10分）
风险控制（15分）：
- 10日波动率 < 近期30日波动率中位数（5分）
- 价格处于20日均线上方且偏离度<8%（5分）
- RSI在40-60之间（5分）
市场环境适配（10分）：
- 近20日市场处于上涨趋势且个股相对强度>1（10分）
- 其他情况（5分）
扣分项（直接从总分扣除）：
- 出现长上影线（扣5分）
- 涨幅>5%但波动率同步放大（扣3分）
- 价涨量缩（扣5分）
- RSI>70或<30（扣5分）
最优阈值分析
买入阈值：值：65
卖出阈值：值：50
"""


@dataclass
class EvalResult:
    symbol: str
    n: int
    train_n: int
    val_n: int
    baseline_buy: float
    baseline_hold: int
    baseline_val_ret_pct: float
    opt_buy: float
    opt_hold: int
    opt_val_ret_pct: float
    bh_val_ret_pct: float


def _normalize_kline(df: pd.DataFrame) -> pd.DataFrame:
    if df is None or df.empty:
        return pd.DataFrame()
    out = df.copy()
    if '日期' in out.columns:
        out['日期'] = pd.to_datetime(out['日期'])
        out = out.set_index('日期')
    else:
        out.index = pd.to_datetime(out.index)
    out = out.sort_index()
    return out


def buy_hold_return_pct(kline: pd.DataFrame, start_dt: pd.Timestamp, end_dt: pd.Timestamp) -> float:
    if kline.empty:
        return 0.0
    kl = kline.copy()
    kl = kl.loc[(kl.index >= start_dt) & (kl.index <= end_dt)]
    if kl.empty:
        return 0.0
    p0 = float(kl['收盘'].iloc[0])
    p1 = float(kl['收盘'].iloc[-1])
    if not p0:
        return 0.0
    return (p1 / p0 - 1) * 100


def eval_one_symbol(symbol: str, start_date: str, end_date: str, horizon_days: int, train_ratio: float, min_val_samples: int):
    kline = _normalize_kline(get_kline(symbol, start_date=start_date, end_date=end_date))
    if kline.empty or len(kline) < 120:
        return None

    parser = ScoreFormulaParser()
    formula_info = parser.parse_deepseek_result(DEFAULT_FORMULA_TEXT)
    scorer = DynamicScorer(formula_info)

    ab = optimize_ab_full_walkforward(
        kline,
        scorer,
        n_splits=3,
        val_ratio=0.2,
        min_trades=max(2, int(min_val_samples // 4)),
        fee_bps=10.0,
        top_k=20,
    )
    folds = ab.get("folds", [])
    if not folds:
        return None
    last = folds[-1]
    val_start = pd.to_datetime(last["验证起"])
    val_end = pd.to_datetime(last["验证止"])

    baseline_buy = float(formula_info.get('thresholds', {}).get('buy', 65.0))
    baseline_sell = float(formula_info.get('thresholds', {}).get('sell', 50.0))
    baseline_hold = int(horizon_days)
    baseline_params = BacktestParams(
        buy_threshold=baseline_buy,
        sell_threshold=baseline_sell,
        max_holding_days=int(baseline_hold),
        take_profit_pct=8.0,
        stop_loss_pct=-5.0,
        fee_bps=10.0,
        price_filter_enabled=True,
    )
    base_res = run_backtest(kline, scorer, baseline_params, start_date=val_start, end_date=val_end)
    base_ret = float(base_res.metrics.get("总收益率%", 0.0))

    winner = ab.get("winner_balance")
    chosen = ab.get("A") if winner == "A" else ab.get("B")
    if chosen is None:
        return None
    p = chosen["params"]
    opt_ret = float(chosen["val"].get("总收益率%", 0.0))

    bh_ret = buy_hold_return_pct(kline, pd.to_datetime(val_start), pd.to_datetime(val_end))

    return EvalResult(
        symbol=symbol,
        n=int(len(kline)),
        train_n=int((kline.index < pd.to_datetime(val_start)).sum()),
        val_n=int((kline.index >= pd.to_datetime(val_start)).sum()),
        baseline_buy=baseline_buy,
        baseline_hold=baseline_hold,
        baseline_val_ret_pct=float(base_ret),
        opt_buy=float(p.buy_threshold),
        opt_hold=int(p.max_holding_days),
        opt_val_ret_pct=float(opt_ret),
        bh_val_ret_pct=float(bh_ret),
    )


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--symbols", default="000001,600000,600519,000333,002594")
    ap.add_argument("--months", type=int, default=12)
    ap.add_argument("--horizon", type=int, default=7)
    ap.add_argument("--train_ratio", type=float, default=0.7)
    ap.add_argument("--min_val_samples", type=int, default=30)
    args = ap.parse_args()

    end_date = datetime.now().strftime("%Y%m%d")
    start_date = (datetime.now() - timedelta(days=int(args.months * 30.5))).strftime("%Y%m%d")
    symbols = [s.strip() for s in args.symbols.split(",") if s.strip()]

    results = []
    for s in symbols:
        r = eval_one_symbol(
            s,
            start_date=start_date,
            end_date=end_date,
            horizon_days=int(args.horizon),
            train_ratio=float(args.train_ratio),
            min_val_samples=int(args.min_val_samples),
        )
        if r is not None:
            results.append(r)

    if not results:
        print("no results")
        return

    df = pd.DataFrame([r.__dict__ for r in results])
    df["improve_pct"] = df["opt_val_ret_pct"] - df["baseline_val_ret_pct"]
    df = df.sort_values("improve_pct", ascending=False)
    with pd.option_context("display.max_rows", 200, "display.max_columns", 200):
        print(df.to_string(index=False))

    avg_improve = float(df["improve_pct"].mean())
    beat_bh = float((df["opt_val_ret_pct"] > df["bh_val_ret_pct"]).mean() * 100)
    print(f"\nsummary: n={len(df)} avg_improve={avg_improve:.3f}% beat_buyhold={beat_bh:.1f}%")


if __name__ == "__main__":
    main()

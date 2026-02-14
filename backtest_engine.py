#!/usr/bin/env python3
# -*- coding: utf-8 -*-

from __future__ import annotations

from dataclasses import dataclass
from typing import Optional

import numpy as np
import pandas as pd


@dataclass(frozen=True)
class BacktestParams:
    buy_threshold: float
    sell_threshold: float
    max_holding_days: int
    take_profit_pct: float
    stop_loss_pct: float
    fee_bps: float = 10.0
    price_filter_enabled: bool = True
    price_to_low_max_pct: float = 25.0
    high_score_override_margin: float = 10.0


@dataclass(frozen=True)
class BacktestResult:
    equity_curve: pd.Series
    trades: list[dict]
    metrics: dict


def _normalize_kline(df: pd.DataFrame) -> pd.DataFrame:
    if df is None or df.empty:
        return pd.DataFrame()
    out = df.copy()
    if "日期" in out.columns:
        out["日期"] = pd.to_datetime(out["日期"])
        out = out.set_index("日期")
    else:
        out.index = pd.to_datetime(out.index)
    return out.sort_index()


def _compute_metrics(equity: pd.Series) -> dict:
    if equity is None or len(equity) < 2:
        return {
            "总收益率%": 0.0,
            "年化收益%": 0.0,
            "年化波动%": 0.0,
            "夏普": 0.0,
            "最大回撤%": 0.0,
            "交易次数": 0,
            "胜率%": 0.0,
            "平均单笔%": 0.0,
        }
    s = equity.dropna().astype(float)
    daily = s.pct_change().fillna(0.0)
    total = (float(s.iloc[-1]) / float(s.iloc[0]) - 1.0) * 100
    years = max(1e-9, (s.index[-1] - s.index[0]).days / 365.0)
    ann = (float(s.iloc[-1]) / float(s.iloc[0])) ** (1.0 / years) - 1.0
    ann_ret = ann * 100
    ann_vol = float(daily.std() * np.sqrt(250) * 100)
    sharpe = float(daily.mean() / daily.std() * np.sqrt(250)) if daily.std() != 0 else 0.0
    dd = (s / s.cummax() - 1.0) * 100
    mdd = float(dd.min())
    return {
        "总收益率%": float(total),
        "年化收益%": float(ann_ret),
        "年化波动%": float(ann_vol),
        "夏普": float(sharpe),
        "最大回撤%": float(mdd),
    }


def run_backtest(
    kline: pd.DataFrame,
    scorer,
    params: BacktestParams,
    lookback_days: int = 30,
    start_date: Optional[pd.Timestamp] = None,
    end_date: Optional[pd.Timestamp] = None,
) -> BacktestResult:
    kl = _normalize_kline(kline)
    if kl.empty:
        return BacktestResult(pd.Series(dtype=float), [], {**_compute_metrics(pd.Series(dtype=float)), "交易次数": 0, "胜率%": 0.0, "平均单笔%": 0.0})

    if start_date is not None:
        kl = kl.loc[kl.index >= pd.to_datetime(start_date)]
    if end_date is not None:
        kl = kl.loc[kl.index <= pd.to_datetime(end_date)]
    kl = kl.sort_index()
    if len(kl) < lookback_days + 5:
        return BacktestResult(pd.Series(dtype=float), [], {**_compute_metrics(pd.Series(dtype=float)), "交易次数": 0, "胜率%": 0.0, "平均单笔%": 0.0})

    fee = float(params.fee_bps) / 10000.0
    buy_th = float(params.buy_threshold)
    sell_th = float(params.sell_threshold)
    take_profit = float(params.take_profit_pct) / 100.0
    stop_loss = float(params.stop_loss_pct) / 100.0
    max_hold = int(max(1, params.max_holding_days))

    holding = False
    entry_price = 0.0
    entry_date = None
    holding_days = 0

    trades: list[dict] = []
    equity_points = []
    capital = 1.0
    equity_points.append((kl.index[0], capital))

    for i in range(len(kl) - lookback_days - 1):
        w = kl.iloc[i:i + lookback_days]
        if len(w) < lookback_days:
            continue
        score, _detail = scorer.calculate_score_detail(w)
        score = float(score)

        cur_close = float(w["收盘"].iloc[-1])
        cur_date = w.index[-1]

        next_row = kl.iloc[i + lookback_days]
        next_open = float(next_row["开盘"])
        next_date = next_row.name

        if not holding:
            if score >= buy_th and next_open > 0:
                allow_buy = True
                if params.price_filter_enabled:
                    recent_window = kl.loc[:cur_date].tail(60)
                    recent_low = float(recent_window["最低"].min()) if not recent_window.empty else float(w["最低"].min())
                    if recent_low > 0:
                        price_to_low = (cur_close - recent_low) / recent_low * 100
                        allow_buy = price_to_low < float(params.price_to_low_max_pct)
                    else:
                        allow_buy = True

                    if not allow_buy and score >= buy_th + float(params.high_score_override_margin):
                        allow_buy = True

                if allow_buy:
                    holding = True
                    entry_price = next_open
                    entry_date = next_date
                    holding_days = 0
                    trades.append({"date": entry_date, "signal": "BUY", "price": entry_price, "return_pct": 0.0, "capital": capital})
        else:
            holding_days += 1
            if entry_price <= 0:
                holding = False
                continue
            pnl = (cur_close - entry_price) / entry_price
            sell_triggered = False
            reason = ""

            if pnl >= take_profit:
                sell_triggered = True
                reason = "TP"
            elif pnl <= stop_loss:
                sell_triggered = True
                reason = "SL"
            elif score <= sell_th:
                sell_triggered = True
                reason = "SCORE"
            elif holding_days >= max_hold:
                sell_triggered = True
                reason = "TIME"

            if sell_triggered and next_open > 0:
                exit_price = next_open
                ret_pct = (exit_price - entry_price) / entry_price * 100
                capital *= (1 + ret_pct / 100.0 - fee)
                equity_points.append((next_date, capital))
                trades.append({"date": next_date, "signal": f"SELL-{reason}", "price": exit_price, "return_pct": ret_pct, "capital": capital})
                holding = False
                entry_price = 0.0
                entry_date = None
                holding_days = 0

    equity = pd.Series([p[1] for p in equity_points], index=pd.to_datetime([p[0] for p in equity_points])).sort_index()
    equity = equity.reindex(kl.index).ffill()
    if len(equity) > 0:
        equity = equity.fillna(1.0)

    m = _compute_metrics(equity)
    sell_trades = [t for t in trades if str(t.get("signal", "")).startswith("SELL")]
    if sell_trades:
        rets = np.array([float(t.get("return_pct", 0.0)) for t in sell_trades], dtype=float)
        win = float((rets > 0).mean() * 100)
        avg = float(rets.mean())
        m["交易次数"] = int(len(sell_trades))
        m["胜率%"] = win
        m["平均单笔%"] = avg
    else:
        m["交易次数"] = 0
        m["胜率%"] = 0.0
        m["平均单笔%"] = 0.0

    return BacktestResult(equity_curve=equity, trades=trades, metrics=m)


#!/usr/bin/env python3
# -*- coding: utf-8 -*-

from __future__ import annotations

from dataclasses import dataclass
from typing import Literal, Optional

import numpy as np
import pandas as pd


Objective = Literal["求高", "求稳", "平衡"]


@dataclass(frozen=True)
class StrategyConfig:
    name: str
    buy_threshold: float
    holding_days: int
    fee_bps: float = 10.0


@dataclass(frozen=True)
class StrategyResult:
    config: StrategyConfig
    metrics: dict
    equity: pd.Series
    trade_count: int


def _max_drawdown_pct(equity: pd.Series) -> float:
    if equity is None or len(equity) < 2:
        return 0.0
    s = equity.dropna().astype(float)
    dd = (s / s.cummax() - 1.0) * 100
    return float(dd.min())


def perf_metrics(equity: pd.Series) -> dict:
    if equity is None or len(equity) < 2:
        return {
            "总收益率%": 0.0,
            "年化收益%": 0.0,
            "年化波动%": 0.0,
            "夏普": 0.0,
            "最大回撤%": 0.0,
        }

    s = equity.dropna().astype(float)
    daily = s.pct_change().fillna(0.0)
    total = (float(s.iloc[-1]) / float(s.iloc[0]) - 1.0) * 100
    years = max(1e-9, (s.index[-1] - s.index[0]).days / 365.0)
    ann = (float(s.iloc[-1]) / float(s.iloc[0])) ** (1.0 / years) - 1.0
    ann_ret = ann * 100
    ann_vol = float(daily.std() * np.sqrt(250) * 100)
    sharpe = float(daily.mean() / daily.std() * np.sqrt(250)) if daily.std() != 0 else 0.0
    mdd = _max_drawdown_pct(s)
    return {
        "总收益率%": float(total),
        "年化收益%": float(ann_ret),
        "年化波动%": float(ann_vol),
        "夏普": float(sharpe),
        "最大回撤%": float(mdd),
    }


def simulate_equity_from_eval_df(
    eval_df: pd.DataFrame,
    buy_threshold: float,
    holding_days: int,
    fee_bps: float = 10.0,
) -> tuple[pd.Series, int]:
    if eval_df is None or eval_df.empty:
        return pd.Series(dtype=float), 0

    df = eval_df.copy()
    df["日期"] = pd.to_datetime(df["日期"])
    df = df.sort_values("日期").reset_index(drop=True)

    capital = 1.0
    points = []
    i = 0
    trades = 0
    fee = float(fee_bps) / 10000.0
    while i < len(df):
        row = df.iloc[i]
        if float(row["评分"]) >= float(buy_threshold):
            r = float(row["未来收益"]) / 100.0
            capital *= (1 + r - fee)
            points.append((row["日期"], capital))
            trades += 1
            i += max(1, int(holding_days))
        else:
            i += 1

    if not points:
        return pd.Series(dtype=float), 0
    s = pd.Series([p[1] for p in points], index=pd.to_datetime([p[0] for p in points])).sort_index()
    return s, trades


def _threshold_candidates(train_df: pd.DataFrame, min_samples: int) -> list[float]:
    scores = train_df["评分"].astype(float)
    qs = np.linspace(0.5, 0.98, 25)
    cands = sorted(set(float(scores.quantile(q)) for q in qs))
    out = []
    for th in cands:
        if int((train_df["评分"] >= th).sum()) >= int(min_samples):
            out.append(float(th))
    return out


def optimize_on_train_pick_on_val(
    eval_df: pd.DataFrame,
    objective: Objective,
    train_ratio: float = 0.7,
    holding_grid: Optional[list[int]] = None,
    min_samples: int = 30,
    fee_bps: float = 10.0,
) -> tuple[StrategyResult, pd.DataFrame]:
    if holding_grid is None:
        holding_grid = [3, 5, 7, 10, 14, 20]

    df = eval_df.copy()
    df["日期"] = pd.to_datetime(df["日期"])
    df = df.sort_values("日期").reset_index(drop=True)

    split = int(len(df) * float(train_ratio))
    train_df = df.iloc[:split].copy()
    val_df = df.iloc[split:].copy()
    if len(val_df) < max(20, int(min_samples)):
        eq, trades = simulate_equity_from_eval_df(val_df, 1000, 1, fee_bps=fee_bps)
        cfg = StrategyConfig(name=f"{objective}-fallback", buy_threshold=1000, holding_days=1, fee_bps=fee_bps)
        res = StrategyResult(config=cfg, metrics=perf_metrics(eq), equity=eq, trade_count=trades)
        return res, pd.DataFrame()

    ths = _threshold_candidates(train_df, min_samples=max(10, int(min_samples)))
    rows = []
    for hold in holding_grid:
        for th in ths:
            eq, trades = simulate_equity_from_eval_df(val_df, th, hold, fee_bps=fee_bps)
            m = perf_metrics(eq)
            score = None
            if objective == "求高":
                score = m["年化收益%"]
            elif objective == "求稳":
                score = m["夏普"] + 0.02 * m["最大回撤%"]
            else:
                score = m["年化收益%"] + 0.5 * m["夏普"] + 0.01 * m["最大回撤%"]
            rows.append(
                {
                    "buy_threshold": float(th),
                    "holding_days": int(hold),
                    "trades": int(trades),
                    "score": float(score),
                    **m,
                }
            )

    grid = pd.DataFrame(rows)
    if grid.empty:
        eq, trades = simulate_equity_from_eval_df(val_df, 1000, 1, fee_bps=fee_bps)
        cfg = StrategyConfig(name=f"{objective}-fallback", buy_threshold=1000, holding_days=1, fee_bps=fee_bps)
        res = StrategyResult(config=cfg, metrics=perf_metrics(eq), equity=eq, trade_count=trades)
        return res, grid

    grid = grid.sort_values(["score", "年化收益%", "夏普"], ascending=False).reset_index(drop=True)
    best = grid.iloc[0].to_dict()
    eq, trades = simulate_equity_from_eval_df(val_df, best["buy_threshold"], best["holding_days"], fee_bps=fee_bps)
    cfg = StrategyConfig(
        name=str(objective),
        buy_threshold=float(best["buy_threshold"]),
        holding_days=int(best["holding_days"]),
        fee_bps=float(fee_bps),
    )
    res = StrategyResult(config=cfg, metrics=perf_metrics(eq), equity=eq, trade_count=trades)
    return res, grid


def ab_test(
    eval_df: pd.DataFrame,
    train_ratio: float = 0.7,
    min_samples: int = 30,
    fee_bps: float = 10.0,
) -> dict:
    a_res, a_grid = optimize_on_train_pick_on_val(
        eval_df, objective="求高", train_ratio=train_ratio, min_samples=min_samples, fee_bps=fee_bps
    )
    b_res, b_grid = optimize_on_train_pick_on_val(
        eval_df, objective="求稳", train_ratio=train_ratio, min_samples=min_samples, fee_bps=fee_bps
    )

    def win_by(o: Objective) -> str:
        if o == "求高":
            return "A(求高)" if a_res.metrics["年化收益%"] >= b_res.metrics["年化收益%"] else "B(求稳)"
        if o == "求稳":
            a_score = a_res.metrics["夏普"] + 0.02 * a_res.metrics["最大回撤%"]
            b_score = b_res.metrics["夏普"] + 0.02 * b_res.metrics["最大回撤%"]
            return "A(求高)" if a_score >= b_score else "B(求稳)"
        a_score = a_res.metrics["年化收益%"] + 0.5 * a_res.metrics["夏普"] + 0.01 * a_res.metrics["最大回撤%"]
        b_score = b_res.metrics["年化收益%"] + 0.5 * b_res.metrics["夏普"] + 0.01 * b_res.metrics["最大回撤%"]
        return "A(求高)" if a_score >= b_score else "B(求稳)"

    return {
        "A": a_res,
        "B": b_res,
        "A_grid": a_grid,
        "B_grid": b_grid,
        "winner_high": win_by("求高"),
        "winner_stable": win_by("求稳"),
        "winner_balance": win_by("平衡"),
    }


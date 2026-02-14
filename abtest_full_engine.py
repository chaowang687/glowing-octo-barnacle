#!/usr/bin/env python3
# -*- coding: utf-8 -*-

from __future__ import annotations

from dataclasses import dataclass
from typing import Literal, Optional

import numpy as np
import pandas as pd

from backtest_engine import BacktestParams, run_backtest


Objective = Literal["求高", "求稳", "平衡"]


@dataclass(frozen=True)
class CandidateSpace:
    buy_thresholds: list[float]
    sell_gap: float
    holding_days: list[int]
    take_profit_pcts: list[float]
    stop_loss_pcts: list[float]
    price_filter_enabled: bool = True


def _objective_score(metrics: dict, objective: Objective) -> float:
    ann = float(metrics.get("年化收益%", 0.0))
    sharpe = float(metrics.get("夏普", 0.0))
    mdd = float(metrics.get("最大回撤%", 0.0))  # negative
    if objective == "求高":
        return ann
    if objective == "求稳":
        return sharpe + 0.02 * mdd
    return ann + 0.5 * sharpe + 0.01 * mdd


def _split_by_time(kline: pd.DataFrame, train_ratio: float) -> tuple[pd.Timestamp, pd.Timestamp, pd.Timestamp, pd.Timestamp]:
    kl = kline.copy()
    if "日期" in kl.columns:
        kl["日期"] = pd.to_datetime(kl["日期"])
        kl = kl.set_index("日期")
    else:
        kl.index = pd.to_datetime(kl.index)
    kl = kl.sort_index()
    split = int(len(kl) * float(train_ratio))
    split = max(60, min(len(kl) - 60, split))
    train_start = kl.index[0]
    train_end = kl.index[split - 1]
    val_start = kl.index[split]
    val_end = kl.index[-1]
    return train_start, train_end, val_start, val_end


def _time_index(kline: pd.DataFrame) -> pd.DatetimeIndex:
    kl = kline.copy()
    if "日期" in kl.columns:
        kl["日期"] = pd.to_datetime(kl["日期"])
        kl = kl.set_index("日期")
    else:
        kl.index = pd.to_datetime(kl.index)
    return pd.DatetimeIndex(kl.sort_index().index)


def _search_one_range(
    kline: pd.DataFrame,
    scorer,
    train_start: pd.Timestamp,
    train_end: pd.Timestamp,
    val_start: pd.Timestamp,
    val_end: pd.Timestamp,
    space: CandidateSpace,
    objective: Objective,
    min_trades: int,
    fee_bps: float,
    top_k: int = 20,
) -> tuple[Optional[dict], pd.DataFrame]:
    rows = []
    cache: dict = {}

    def _run(params: BacktestParams, start_dt: pd.Timestamp, end_dt: pd.Timestamp):
        key = (
            round(params.buy_threshold, 4),
            round(params.sell_threshold, 4),
            int(params.max_holding_days),
            round(params.take_profit_pct, 4),
            round(params.stop_loss_pct, 4),
            round(params.fee_bps, 4),
            bool(params.price_filter_enabled),
            pd.to_datetime(start_dt),
            pd.to_datetime(end_dt),
        )
        v = cache.get(key)
        if v is not None:
            return v
        r = run_backtest(kline, scorer, params, start_date=start_dt, end_date=end_dt)
        cache[key] = r
        return r

    for buy in space.buy_thresholds:
        sell = float(buy) - float(space.sell_gap)
        sell = max(0.0, min(100.0, sell))
        for hold in space.holding_days:
            for tp in space.take_profit_pcts:
                for sl in space.stop_loss_pcts:
                    params = BacktestParams(
                        buy_threshold=float(buy),
                        sell_threshold=float(sell),
                        max_holding_days=int(hold),
                        take_profit_pct=float(tp),
                        stop_loss_pct=float(sl),
                        fee_bps=float(fee_bps),
                        price_filter_enabled=bool(space.price_filter_enabled),
                    )
                    train_res = _run(params, train_start, train_end)
                    if int(train_res.metrics.get("交易次数", 0)) < int(min_trades):
                        continue
                    score = _objective_score(train_res.metrics, objective)
                    rows.append(
                        {
                            "buy": float(buy),
                            "sell": float(sell),
                            "hold": int(hold),
                            "tp": float(tp),
                            "sl": float(sl),
                            "train_score": float(score),
                            "train_ann": float(train_res.metrics.get("年化收益%", 0.0)),
                            "train_sharpe": float(train_res.metrics.get("夏普", 0.0)),
                            "train_mdd": float(train_res.metrics.get("最大回撤%", 0.0)),
                            "train_trades": int(train_res.metrics.get("交易次数", 0)),
                        }
                    )

    grid = pd.DataFrame(rows)
    if grid.empty:
        return None, grid

    grid = grid.sort_values(["train_score", "train_ann", "train_sharpe"], ascending=False).reset_index(drop=True)
    top_k = max(1, min(int(top_k), len(grid)))
    best = None
    best_val_res = None
    best_val_score = None
    for i in range(top_k):
        cand = grid.iloc[i].to_dict()
        cand_params = BacktestParams(
            buy_threshold=float(cand["buy"]),
            sell_threshold=float(cand["sell"]),
            max_holding_days=int(cand["hold"]),
            take_profit_pct=float(cand["tp"]),
            stop_loss_pct=float(cand["sl"]),
            fee_bps=float(fee_bps),
            price_filter_enabled=bool(space.price_filter_enabled),
        )
        val_res = _run(cand_params, val_start, val_end)
        if int(val_res.metrics.get("交易次数", 0)) < max(1, int(min_trades // 2)):
            continue
        val_score = _objective_score(val_res.metrics, objective)
        grid.loc[i, "val_score"] = float(val_score)
        grid.loc[i, "val_ann"] = float(val_res.metrics.get("年化收益%", 0.0))
        grid.loc[i, "val_sharpe"] = float(val_res.metrics.get("夏普", 0.0))
        grid.loc[i, "val_mdd"] = float(val_res.metrics.get("最大回撤%", 0.0))
        grid.loc[i, "val_trades"] = int(val_res.metrics.get("交易次数", 0))

        if best is None or val_score > best_val_score:
            best = cand
            best_val_res = val_res
            best_val_score = float(val_score)

    if best is None:
        best = grid.iloc[0].to_dict()
        cand_params = BacktestParams(
            buy_threshold=float(best["buy"]),
            sell_threshold=float(best["sell"]),
            max_holding_days=int(best["hold"]),
            take_profit_pct=float(best["tp"]),
            stop_loss_pct=float(best["sl"]),
            fee_bps=float(fee_bps),
            price_filter_enabled=bool(space.price_filter_enabled),
        )
        best_val_res = _run(cand_params, val_start, val_end)

    best_params = BacktestParams(
        buy_threshold=float(best["buy"]),
        sell_threshold=float(best["sell"]),
        max_holding_days=int(best["hold"]),
        take_profit_pct=float(best["tp"]),
        stop_loss_pct=float(best["sl"]),
        fee_bps=float(fee_bps),
        price_filter_enabled=bool(space.price_filter_enabled),
    )
    best_train_res = _run(best_params, train_start, train_end)
    out = {
        "params": best_params,
        "train": best_train_res.metrics,
        "val": best_val_res.metrics,
        "val_equity": best_val_res.equity_curve,
        "grid": grid,
    }
    return out, grid


def optimize_ab_full(
    kline: pd.DataFrame,
    scorer,
    train_ratio: float = 0.7,
    min_trades: int = 8,
    fee_bps: float = 10.0,
    objective_a: Objective = "求高",
    objective_b: Objective = "求稳",
    space_a: Optional[CandidateSpace] = None,
    space_b: Optional[CandidateSpace] = None,
) -> dict:
    if space_a is None:
        space_a = CandidateSpace(
            buy_thresholds=[30, 35, 40, 45, 50, 55, 60],
            sell_gap=15,
            holding_days=[3, 5, 7],
            take_profit_pcts=[6, 8, 10],
            stop_loss_pcts=[-4, -5, -6],
            price_filter_enabled=True,
        )
    if space_b is None:
        space_b = CandidateSpace(
            buy_thresholds=[40, 45, 50, 55, 60, 65, 70],
            sell_gap=10,
            holding_days=[7, 10, 14],
            take_profit_pcts=[6, 8, 10],
            stop_loss_pcts=[-3, -4, -5],
            price_filter_enabled=True,
        )

    train_start, train_end, val_start, val_end = _split_by_time(kline, train_ratio=float(train_ratio))
    a, a_grid = _search_one_range(
        kline,
        scorer,
        train_start=train_start,
        train_end=train_end,
        val_start=val_start,
        val_end=val_end,
        space=space_a,
        objective=objective_a,
        min_trades=int(min_trades),
        fee_bps=float(fee_bps),
    )
    b, b_grid = _search_one_range(
        kline,
        scorer,
        train_start=train_start,
        train_end=train_end,
        val_start=val_start,
        val_end=val_end,
        space=space_b,
        objective=objective_b,
        min_trades=int(min_trades),
        fee_bps=float(fee_bps),
    )

    def pick_winner(target: Objective) -> str:
        if a is None and b is None:
            return "NONE"
        if a is None:
            return "B"
        if b is None:
            return "A"
        a_score = _objective_score(a["val"], target)
        b_score = _objective_score(b["val"], target)
        return "A" if a_score >= b_score else "B"

    return {
        "split": {"train_start": train_start, "train_end": train_end, "val_start": val_start, "val_end": val_end},
        "A": a,
        "B": b,
        "winner_high": pick_winner("求高"),
        "winner_stable": pick_winner("求稳"),
        "winner_balance": pick_winner("平衡"),
        "A_grid": a_grid,
        "B_grid": b_grid,
    }


def optimize_ab_full_walkforward(
    kline: pd.DataFrame,
    scorer,
    n_splits: int = 3,
    val_ratio: float = 0.2,
    min_train_bars: int = 160,
    min_trades: int = 8,
    fee_bps: float = 10.0,
    objective_a: Objective = "求高",
    objective_b: Objective = "求稳",
    space_a: Optional[CandidateSpace] = None,
    space_b: Optional[CandidateSpace] = None,
    top_k: int = 20,
) -> dict:
    if space_a is None:
        space_a = CandidateSpace(
            buy_thresholds=[30, 35, 40, 45, 50, 55, 60],
            sell_gap=15,
            holding_days=[3, 5, 7],
            take_profit_pcts=[6, 8, 10],
            stop_loss_pcts=[-4, -5, -6],
            price_filter_enabled=True,
        )
    if space_b is None:
        space_b = CandidateSpace(
            buy_thresholds=[40, 45, 50, 55, 60, 65, 70],
            sell_gap=10,
            holding_days=[7, 10, 14],
            take_profit_pcts=[6, 8, 10],
            stop_loss_pcts=[-3, -4, -5],
            price_filter_enabled=True,
        )

    idx = _time_index(kline)
    if len(idx) < min_train_bars + 80:
        return {"folds": [], "stability": {}, "A": None, "B": None, "winner_high": "NONE", "winner_stable": "NONE", "winner_balance": "NONE"}

    n_splits = max(2, int(n_splits))
    val_len = max(40, int(len(idx) * float(val_ratio)))
    folds = []
    for f in range(n_splits):
        val_end_i = len(idx) - 1 - f * val_len
        val_start_i = val_end_i - val_len + 1
        if val_start_i <= 0:
            continue
        train_end_i = val_start_i - 1
        if train_end_i < int(min_train_bars):
            continue
        train_start = idx[0]
        train_end = idx[train_end_i]
        val_start = idx[val_start_i]
        val_end = idx[val_end_i]
        folds.append((n_splits - f, train_start, train_end, val_start, val_end))
    folds = list(reversed(folds))

    fold_rows = []
    a_folds = []
    b_folds = []
    for fold_id, train_start, train_end, val_start, val_end in folds:
        a, _ = _search_one_range(
            kline,
            scorer,
            train_start=train_start,
            train_end=train_end,
            val_start=val_start,
            val_end=val_end,
            space=space_a,
            objective=objective_a,
            min_trades=int(min_trades),
            fee_bps=float(fee_bps),
            top_k=int(top_k),
        )
        b, _ = _search_one_range(
            kline,
            scorer,
            train_start=train_start,
            train_end=train_end,
            val_start=val_start,
            val_end=val_end,
            space=space_b,
            objective=objective_b,
            min_trades=int(min_trades),
            fee_bps=float(fee_bps),
            top_k=int(top_k),
        )
        a_folds.append(a)
        b_folds.append(b)

        row = {
            "折": int(fold_id),
            "训练起": pd.to_datetime(train_start).date().isoformat(),
            "训练止": pd.to_datetime(train_end).date().isoformat(),
            "验证起": pd.to_datetime(val_start).date().isoformat(),
            "验证止": pd.to_datetime(val_end).date().isoformat(),
        }
        if a is not None:
            p = a["params"]
            m = a["val"]
            row.update({
                "A_年化%": float(m.get("年化收益%", 0.0)),
                "A_夏普": float(m.get("夏普", 0.0)),
                "A_回撤%": float(m.get("最大回撤%", 0.0)),
                "A_交易": int(m.get("交易次数", 0)),
                "A_buy": float(p.buy_threshold),
                "A_hold": int(p.max_holding_days),
            })
        if b is not None:
            p = b["params"]
            m = b["val"]
            row.update({
                "B_年化%": float(m.get("年化收益%", 0.0)),
                "B_夏普": float(m.get("夏普", 0.0)),
                "B_回撤%": float(m.get("最大回撤%", 0.0)),
                "B_交易": int(m.get("交易次数", 0)),
                "B_buy": float(p.buy_threshold),
                "B_hold": int(p.max_holding_days),
            })
        fold_rows.append(row)

    def _scores(side_list: list[Optional[dict]], target: Objective) -> list[float]:
        out = []
        for it in side_list:
            if it is None:
                continue
            out.append(_objective_score(it["val"], target))
        return out

    def _stability(side_list: list[Optional[dict]], target: Objective) -> dict:
        s = _scores(side_list, target)
        if not s:
            return {"median": 0.0, "std": 0.0, "stability": -1e9, "n": 0}
        med = float(np.median(s))
        std = float(np.std(s))
        return {"median": med, "std": std, "stability": med - 0.5 * std, "n": int(len(s))}

    stability = {
        "A": {
            "求高": _stability(a_folds, "求高"),
            "求稳": _stability(a_folds, "求稳"),
            "平衡": _stability(a_folds, "平衡"),
        },
        "B": {
            "求高": _stability(b_folds, "求高"),
            "求稳": _stability(b_folds, "求稳"),
            "平衡": _stability(b_folds, "平衡"),
        },
    }

    def _winner(target: Objective) -> str:
        a_s = float(stability["A"][target]["stability"])
        b_s = float(stability["B"][target]["stability"])
        if a_s == -1e9 and b_s == -1e9:
            return "NONE"
        return "A" if a_s >= b_s else "B"

    winner_high = _winner("求高")
    winner_stable = _winner("求稳")
    winner_balance = _winner("平衡")

    def _pick_rep(side_list: list[Optional[dict]], target: Objective) -> Optional[dict]:
        best = None
        best_score = None
        for it in side_list:
            if it is None:
                continue
            sc = _objective_score(it["val"], target)
            if best is None or sc > best_score:
                best = it
                best_score = sc
        return best

    a_rep = _pick_rep(a_folds, "平衡")
    b_rep = _pick_rep(b_folds, "平衡")

    return {
        "folds": fold_rows,
        "stability": stability,
        "A": a_rep,
        "B": b_rep,
        "winner_high": winner_high,
        "winner_stable": winner_stable,
        "winner_balance": winner_balance,
    }

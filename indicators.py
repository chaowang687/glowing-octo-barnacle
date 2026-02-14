#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
技术指标计算模块
统一管理所有技术指标的计算
"""

import pandas as pd
import numpy as np
from typing import Dict, List, Optional


def calculate_ma(df: pd.DataFrame, periods: List[int] = None) -> pd.DataFrame:
    """
    计算移动平均线
    
    Args:
        df: 包含收盘价的DataFrame，列名为'收盘'
        periods: 均线周期列表，默认[5, 10, 20, 60, 120, 250]
    
    Returns:
        包含均线的DataFrame
    """
    if periods is None:
        periods = [5, 10, 20, 60, 120, 250]
    
    result = df.copy()
    for period in periods:
        result[f'MA{period}'] = result['收盘'].rolling(window=period).mean()
    return result


def calculate_ema(df: pd.DataFrame, periods: List[int] = None) -> pd.DataFrame:
    """
    计算指数移动平均线
    
    Args:
        df: 包含收盘价的DataFrame，列名为'收盘'
        periods: EMA周期列表，默认[12, 26]
    
    Returns:
        包含EMA的DataFrame
    """
    if periods is None:
        periods = [12, 26]
    
    result = df.copy()
    for period in periods:
        result[f'EMA{period}'] = result['收盘'].ewm(span=period, adjust=False).mean()
    return result


def calculate_macd(df: pd.DataFrame, fast_period: int = 12, slow_period: int = 26, signal_period: int = 9) -> pd.DataFrame:
    """
    计算MACD指标
    
    Args:
        df: 包含收盘价的DataFrame，列名为'收盘'
        fast_period: 快速EMA周期，默认12
        slow_period: 慢速EMA周期，默认26
        signal_period: 信号EMA周期，默认9
    
    Returns:
        包含MACD指标的DataFrame
    """
    result = df.copy()
    
    # 计算EMA
    result[f'EMA{fast_period}'] = result['收盘'].ewm(span=fast_period, adjust=False).mean()
    result[f'EMA{slow_period}'] = result['收盘'].ewm(span=slow_period, adjust=False).mean()
    
    # 计算DIF
    result['DIF'] = result[f'EMA{fast_period}'] - result[f'EMA{slow_period}']
    
    # 计算DEA
    result['DEA'] = result['DIF'].ewm(span=signal_period, adjust=False).mean()
    
    # 计算MACD柱状图
    result['MACD'] = (result['DIF'] - result['DEA']) * 2
    
    return result


def calculate_kdj(df: pd.DataFrame, period: int = 9, k_period: int = 3, d_period: int = 3) -> pd.DataFrame:
    """
    计算KDJ指标
    
    Args:
        df: 包含最高价、最低价、收盘价的DataFrame，列名分别为'最高'、'最低'、'收盘'
        period: RSV周期，默认9
        k_period: K值平滑周期，默认3
        d_period: D值平滑周期，默认3
    
    Returns:
        包含KDJ指标的DataFrame
    """
    result = df.copy()
    
    # 计算RSV
    low_low = result['最低'].rolling(window=period).min()
    high_high = result['最高'].rolling(window=period).max()
    result['RSV'] = (result['收盘'] - low_low) / (high_high - low_low) * 100
    
    # 计算K值
    result['K'] = result['RSV'].ewm(span=k_period, adjust=False).mean()
    
    # 计算D值
    result['D'] = result['K'].ewm(span=d_period, adjust=False).mean()
    
    # 计算J值
    result['J'] = 3 * result['K'] - 2 * result['D']
    
    return result


def calculate_obv(df: pd.DataFrame) -> pd.DataFrame:
    """
    计算OBV（能量潮）指标
    
    Args:
        df: 包含收盘价和成交量的DataFrame，列名分别为'收盘'、'成交量'
    
    Returns:
        包含OBV指标的DataFrame
    """
    result = df.copy()
    
    # 计算价格变化方向
    result['PRICE_CHANGE'] = result['收盘'].diff()
    result['PRICE_DIRECTION'] = np.sign(result['PRICE_CHANGE'])
    
    # 计算OBV
    result['OBV'] = (result['PRICE_DIRECTION'] * result['成交量']).fillna(0).cumsum()
    
    return result


def calculate_cpv(df: pd.DataFrame) -> pd.DataFrame:
    """
    计算CPV（成交量价格验证）指标
    
    Args:
        df: 包含收盘价和成交量的DataFrame，列名分别为'收盘'、'成交量'
    
    Returns:
        包含CPV指标的DataFrame
    """
    result = df.copy()
    
    # 计算价格变化
    result['PRICE_CHANGE'] = result['收盘'].diff()
    result['PRICE_DIRECTION'] = np.sign(result['PRICE_CHANGE'])
    
    # 计算成交量变化
    result['VOLUME_CHANGE'] = result['成交量'].diff()
    result['VOLUME_DIRECTION'] = np.sign(result['VOLUME_CHANGE'])
    
    # CPV评分：量价同向为正向，异向为负向
    result['CPV_SCORE'] = np.where(
        result['PRICE_DIRECTION'] == result['VOLUME_DIRECTION'],
        1, -1
    )
    
    # CPV连续正向计数
    result['CPV_STREAK'] = result['CPV_SCORE'].groupby(
        (result['CPV_SCORE'] != result['CPV_SCORE'].shift()).cumsum()
    ).cumcount() + 1
    
    return result


def calculate_volume_indicators(df: pd.DataFrame) -> pd.DataFrame:
    """
    计算成交量相关指标
    
    Args:
        df: 包含成交量的DataFrame，列名为'成交量'
    
    Returns:
        包含成交量指标的DataFrame
    """
    result = df.copy()
    
    # 成交量均线
    result['VOL_MA5'] = result['成交量'].rolling(window=5).mean()
    result['VOL_MA10'] = result['成交量'].rolling(window=10).mean()
    
    # 放量缩量比
    result['VOL_RATIO'] = result['成交量'] / result['VOL_MA5']
    
    return result


def calculate_all_indicators(df: pd.DataFrame) -> pd.DataFrame:
    """
    计算所有技术指标
    
    Args:
        df: 包含基本数据的DataFrame，需要有以下列：
            - 收盘
            - 最高
            - 最低
            - 成交量
    
    Returns:
        包含所有技术指标的DataFrame
    """
    result = df.copy()
    
    # 计算MA
    result = calculate_ma(result)
    
    # 计算MACD
    result = calculate_macd(result)
    
    # 计算KDJ
    result = calculate_kdj(result)
    
    # 计算OBV
    result = calculate_obv(result)
    
    # 计算成交量指标
    result = calculate_volume_indicators(result)
    
    # 计算CPV
    result = calculate_cpv(result)
    
    return result


def get_indicator_status(df: pd.DataFrame) -> Dict[str, str]:
    """
    获取技术指标状态
    
    Args:
        df: 包含技术指标的DataFrame
    
    Returns:
        技术指标状态字典
    """
    status = {}
    
    # 均线状态
    if 'MA20' in df.columns and 'MA60' in df.columns:
        ma_status = "多头↑" if df['MA20'].iloc[-1] > df['MA60'].iloc[-1] else "空头↓"
        status['ma_status'] = ma_status
    
    # MACD状态
    if 'MACD' in df.columns:
        macd_status = "金叉↑" if df['MACD'].iloc[-1] > 0 else "死叉↓"
        status['macd_status'] = macd_status
    
    # KDJ状态
    if 'J' in df.columns:
        if df['J'].iloc[-1] > 100:
            kdj_status = "超买"
        elif df['J'].iloc[-1] < 0:
            kdj_status = "超卖"
        else:
            kdj_status = "正常"
        status['kdj_status'] = kdj_status
    
    # OBV状态
    if 'OBV' in df.columns:
        obv_trend = "上涨↑" if df['OBV'].iloc[-1] > df['OBV'].iloc[-20] else "下跌↓"
        status['obv_trend'] = obv_trend
    
    return status


# 便捷函数
def add_indicators(df: pd.DataFrame) -> pd.DataFrame:
    """
    向DataFrame添加技术指标
    
    Args:
        df: 包含基本数据的DataFrame
    
    Returns:
        包含技术指标的DataFrame
    """
    return calculate_all_indicators(df)


def get_technical_status(df: pd.DataFrame) -> Dict[str, str]:
    """
    获取技术指标状态
    
    Args:
        df: 包含技术指标的DataFrame
    
    Returns:
        技术指标状态字典
    """
    return get_indicator_status(df)

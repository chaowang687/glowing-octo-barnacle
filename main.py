#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
A股量化选股系统 - 主程序
功能：获取A股数据、技术分析、缠论结构分析、选股筛选
"""

import os
import warnings
warnings.filterwarnings('ignore')

# 禁用代理（如果有的话）
for var in ['HTTP_PROXY', 'HTTPS_PROXY', 'http_proxy', 'https_proxy', 'ALL_PROXY', 'all_proxy']:
    os.environ.pop(var, None)
os.environ['no_proxy'] = '*'

import akshare as ak
import pandas as pd
import numpy as np
from datetime import datetime, timedelta

# ==================== 数据获取模块 ====================

def get_realtime_quotes():
    """获取A股实时行情"""
    print("📊 正在获取A股实时行情...")
    try:
        df = ak.stock_zh_a_spot_em()
        print(f"✅ 成功获取 {len(df)} 只股票")
        return df
    except Exception as e:
        print(f"❌ 获取失败: {e}")
        return None


def get_stock_kline(symbol, period='daily', start_date=None, end_date=None):
    """获取个股K线数据
    
    Args:
        symbol: 股票代码，如 '000001'
        period: 'daily' | 'weekly' | 'monthly'
        start_date: 开始日期 'YYYYMMDD'
        end_date: 结束日期 'YYYYMMDD'
    """
    if start_date is None:
        start_date = (datetime.now() - timedelta(days=365)).strftime('%Y%m%d')
    if end_date is None:
        end_date = datetime.now().strftime('%Y%m%d')
    
    try:
        df = ak.stock_zh_a_hist(symbol=symbol, period=period, 
                                start_date=start_date, end_date=end_date,
                                adjust="qfq")
        return df
    except Exception as e:
        print(f"获取 {symbol} K线失败: {e}")
        return None


def get_stock_info(symbol):
    """获取股票基本信息"""
    try:
        df = ak.stock_individual_info_em(symbol=symbol)
        info = {}
        for _, row in df.iterrows():
            info[row['item']] = row['value']
        return info
    except Exception as e:
        print(f"获取 {symbol} 基本信息失败: {e}")
        return None


# ==================== 技术指标模块 ====================

def calculate_ma(df, periods=[5, 10, 20, 60, 120, 250]):
    """计算移动平均线"""
    result = df.copy()
    for period in periods:
        result[f'MA{period}'] = result['收盘'].rolling(window=period).mean()
    return result


def calculate_ema(df, periods=[12, 26]):
    """计算指数移动平均线"""
    result = df.copy()
    for period in periods:
        result[f'EMA{period}'] = result['收盘'].ewm(span=period, adjust=False).mean()
    # 计算MACD
    result['DIF'] = result['EMA12'] - result['EMA26']
    result['DEA'] = result['DIF'].ewm(span=9, adjust=False).mean()
    result['MACD'] = (result['DIF'] - result['DEA']) * 2
    return result


def calculate_volume_indicators(df):
    """计算成交量指标"""
    result = df.copy()
    
    # OBV能量潮
    result['OBV'] = (np.sign(result['收盘'].diff()) * result['成交量']).fillna(0).cumsum()
    
    # 成交量均线
    result['VOL_MA5'] = result['成交量'].rolling(window=5).mean()
    result['VOL_MA10'] = result['成交量'].rolling(window=10).mean()
    
    # 放量缩量比
    result['VOL_RATIO'] = result['成交量'] / result['VOL_MA5']
    
    return result


def calculate_cpv(df):
    """计算CPV（成交量价格验证）指标
    
    CPV核心思想：
    - 价格上涨时，成交量应该放大
    - 价格下跌时，成交量应该萎缩
    - 量价配合才是健康的走势
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


# ==================== 缠论基础模块 ====================

def handle_inclusion(kline_data):
    """处理K线包含关系
    
    包含关系：两根K线，一根完全包含另一根
    处理规则：
    - 向上处理：取高点的高点，低点的高点
    - 向下处理：取高点的低点，低点的低点
    """
    df = kline_data.copy()
    n = len(df)
    
    # 预处理：标记包含关系
    include_flags = []
    direction = 0  # 0: 无方向, 1: 向上, -1: 向下
    
    for i in range(n):
        if i < 2:
            include_flags.append(0)
            continue
            
        curr_high = df.iloc[i]['high']
        curr_low = df.iloc[i]['low']
        prev_high = df.iloc[i-1]['high']
        prev_low = df.iloc[i-1]['low']
        
        # 判断包含关系
        if (curr_high >= prev_high and curr_low <= prev_low) or \
           (curr_high <= prev_high and curr_low >= prev_low):
            
            if direction == 0:
                # 首次包含，根据前后方向决定
                if i >= 2:
                    before_high = df.iloc[i-2]['high']
                    before_low = df.iloc[i-2]['low']
                    if before_high <= prev_high and before_low >= prev_low:
                        direction = 1  # 向上
                    else:
                        direction = -1  # 向下
            
            # 处理包含
            if direction == 1:  # 向上处理
                new_high = max(curr_high, prev_high)
                new_low = max(curr_low, prev_low)
            else:  # 向下处理
                new_high = min(curr_high, prev_high)
                new_low = min(curr_low, prev_low)
            
            df.iloc[i-1, df.columns.get_loc('high')] = new_high
            df.iloc[i-1, df.columns.get_loc('low')] = new_low
            df.iloc[i, df.columns.get_loc('high')] = new_high
            df.iloc[i, df.columns.get_loc('low')] = new_low
            
            include_flags.append(1)
        else:
            direction = 0
            include_flags.append(0)
    
    df['include'] = include_flags
    return df


def identify_fractals(kline_data):
    """识别顶分型和底分型
    
    顶分型：中间K线的高点最高，低点也在相邻两根之上
    底分型：中间K线的低点最低，高点也在相邻两根之下
    """
    df = handle_inclusion(kline_data)
    n = len(df)
    
    fractal_top = [0] * n  # 顶分型标记
    fractal_bottom = [0] * n  # 底分型标记
    
    for i in range(2, n - 2):
        # 顶分型判断
        if (df.iloc[i-2]['high'] < df.iloc[i-1]['high'] > df.iloc[i]['high'] and
            df.iloc[i-2]['low'] < df.iloc[i-1]['low'] > df.iloc[i]['low']):
            fractal_top[i-1] = 1
        
        # 底分型判断
        if (df.iloc[i-2]['high'] > df.iloc[i-1]['high'] < df.iloc[i]['high'] and
            df.iloc[i-2]['low'] > df.iloc[i-1]['low'] < df.iloc[i]['low']):
            fractal_bottom[i-1] = 1
    
    df['fractal_top'] = fractal_top
    df['fractal_bottom'] = fractal_bottom
    
    return df


# ==================== 选股筛选模块 ====================

def filter_by_technical(df, conditions=None):
    """技术面筛选
    
    conditions: 筛选条件字典
        - min_ma20_above_ma60: MA20 > MA60 (多头排列)
        - min_volume: 最小成交量
        - min_change: 最小涨跌幅
    """
    if conditions is None:
        conditions = {
            'min_ma20_ma60': True,  # 多头排列
            'min_change': 0,         # 最小涨幅
            'min_volume': 5000,      # 最小成交量(万)
        }
    
    result = df.copy()
    
    # 涨跌幅筛选
    if '涨跌幅' in result.columns:
        result = result[result['涨跌幅'] > conditions.get('min_change', 0)]
    
    # 成交量筛选
    if '成交额' in result.columns:
        result = result[result['成交额'] > conditions.get('min_volume', 5000) * 10000]
    elif '成交量' in result.columns:
        result = result[result['成交量'] > conditions.get('min_volume', 5000)]
    
    return result


def filter_by_fundamentals(symbols, conditions=None):
    """基本面筛选
    
    conditions: 筛选条件字典
        - max_pe: 最大市盈率
        - min_roe: 最小ROE
    """
    if conditions is None:
        conditions = {
            'max_pe': 50,
            'min_roe': 5,
        }
    
    filtered = []
    
    for symbol in symbols:
        info = get_stock_info(symbol)
        if info is None:
            continue
        
        try:
            pe = float(info.get('市盈率', 0))
            roe = float(info.get('净资产收益率', 0))
            
            if pe < conditions.get('max_pe', 50) and roe > conditions.get('min_roe', 5):
                filtered.append(symbol)
        except:
            continue
    
    return filtered


def calculate_comprehensive_score(stock_df, kline_data):
    """综合评分
    
    评分因素：
    - 趋势强度（均线多头排列）
    - 动量（涨幅）
    - 成交量配合（OBV）
    - 缠论结构
    """
    score = 0
    
    # 趋势评分
    if 'MA20' in kline_data.columns and 'MA60' in kline_data.columns:
        if kline_data['MA20'].iloc[-1] > kline_data['MA60'].iloc[-1]:
            score += 30
    
    # 动量评分
    if '涨跌幅' in stock_df.columns:
        change = float(stock_df['涨跌幅'].iloc[0])
        score += min(change * 10, 30)
    
    # 成交量评分
    if 'OBV' in kline_data.columns:
        obv_trend = kline_data['OBV'].iloc[-1] / kline_data['OBV'].iloc[-20] if len(kline_data) > 20 else 1
        if obv_trend > 1:
            score += 20
    
    return score


# ==================== 主程序 ====================

def main():
    print("=" * 60)
    print("🏆 A股全能量化选股系统")
    print("=" * 60)
    
    # 1. 获取实时行情
    print("\n📥 步骤1: 获取实时行情数据...")
    df = get_realtime_quotes()
    
    if df is None:
        print("❌ 无法获取数据，请检查网络连接")
        return
    
    # 2. 技术面筛选
    print("\n🔍 步骤2: 技术面筛选...")
    filtered = filter_by_technical(df, {
        'min_change': 3,       # 涨幅大于3%
        'min_volume': 5000,    # 成交额大于5000万
    })
    print(f"   技术面筛选后: {len(filtered)} 只股票")
    
    # 3. 打印结果
    print("\n📊 筛选结果:")
    if len(filtered) > 0:
        cols = ['代码', '名称', '最新价', '涨跌幅', '成交额', '换手率']
        available_cols = [c for c in cols if c in filtered.columns]
        print(filtered[available_cols].head(20))
    else:
        print("   暂无符合条件的股票")
    
    # 4. 获取单只股票详细分析
    print("\n📈 示例: 获取单只股票K线...")
    sample_code = '000001'  # 平安银行
    kline = get_stock_kline(sample_code)
    
    if kline is not None:
        print(f"   获取到 {sample_code} 的 {len(kline)} 条K线数据")
        
        # 计算技术指标
        kline = calculate_ma(kline)
        kline = calculate_ema(kline)
        kline = calculate_volume_indicators(kline)
        kline = calculate_cpv(kline)
        
        print(f"   MA20: {kline['MA20'].iloc[-1]:.2f}")
        print(f"   MACD: {kline['MACD'].iloc[-1]:.2f}")
        
        # 缠论分析
        kline_renamed = kline.rename(columns={
            '日期': 'date', '开盘': 'open', '收盘': 'close', 
            '最高': 'high', '最低': 'low', '成交量': 'volume'
        })
        kline_analysis = identify_fractals(kline_renamed)
        
        fractal_tops = kline_analysis[kline_analysis['fractal_top'] == 1]
        fractal_bottoms = kline_analysis[kline_analysis['fractal_bottom'] == 1]
        
        print(f"   顶分型数量: {len(fractal_tops)}")
        print(f"   底分型数量: {len(fractal_bottoms)}")
    
    print("\n✅ 程序执行完成!")
    print("=" * 60)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
A股量化选股系统 - Streamlit网页界面
功能：交互式选股、股票分析、K线展示
"""

import os
# 禁用代理
for var in ['HTTP_PROXY', 'HTTPS_PROXY', 'http_proxy', 'https_proxy']:
    os.environ.pop(var, None)
os.environ['no_proxy'] = '*'

import streamlit as st
import pandas as pd
import numpy as np
from datetime import datetime, timedelta
import plotly.graph_objects as go
from plotly.subplots import make_subplots

# 导入自己的数据模块
from data_source import EastMoneyData
from selector import ComprehensiveSelector
from sector_analysis import SectorAnalysis
from tencent_source import TencentDataSource

# 初始化数据对象
em = EastMoneyData()
selector = ComprehensiveSelector()
sector_analysis = SectorAnalysis()
tencent = TencentDataSource()

# 演示模式 - 设置为False使用真实数据
DEMO_MODE = False

# 页面配置
st.set_page_config(
    page_title="A股量化选股系统",
    page_icon="📈",
    layout="wide",
    initial_sidebar_state="expanded"
)

# 样式设置 - 高对比度版本
st.markdown("""
<style>
    /* 背景颜色 */
    .main {
        background-color: #000000;
    }
    .stApp {
        background-color: #000000;
    }
    /* 文字颜色 - 高对比度 */
    body, .stMarkdown, p, div, span {
        color: #FFFFFF !important;
    }
    /* 标题 */
    .title {
        font-size: 32px;
        font-weight: bold;
        color: #FF6B6B !important;
    }
    .subtitle {
        font-size: 18px;
        color: #FFFFFF !important;
    }
    /* 侧边栏文字 */
    .css-17lntkn, .css-16huue {
        color: #FFFFFF !important;
    }
    /* 表格文字 */
    .dataframe {
        color: #FFFFFF !important;
    }
    /* Metric数字 */
    [data-testid="stMetricValue"] {
        color: #4ADE80 !important;
    }
    /* 输入框文字 */
    .stTextInput input {
        color: #FFFFFF !important;
    }
</style>
"""

# ==================== 数据获取函数 ====================

def get_demo_data():
    """生成演示数据"""
    import random
    np.random.seed(42)
    
    # 模拟股票列表
    stocks = []
    for i in range(50):
        code = f"{np.random.randint(0, 9999):06d}"
        name = f"股票{i+1:02d}"
        price = np.random.uniform(5, 100)
        change = np.random.uniform(-8, 10)
        stocks.append({
            '代码': code,
            '名称': name,
            '最新价': round(price, 2),
            '涨跌幅': round(change, 2),
            '涨跌额': round(price * change / 100, 2),
            '成交量': np.random.randint(1000000, 100000000),
            '成交额': np.random.randint(10000000, 1000000000),
            '振幅': round(np.random.uniform(0, 10), 2),
            '换手率': round(np.random.uniform(0, 20), 2),
            '市盈率': round(np.random.uniform(5, 100), 2),
            '市净率': round(np.random.uniform(0.5, 10), 2),
        })
    
    return pd.DataFrame(stocks)


@st.cache_data(ttl=120)
def get_realtime_quotes():
    """获取实时行情"""
    if DEMO_MODE:
        return get_demo_data()
    
    try:
        # 使用腾讯财经数据源
        df = tencent.get_realtime_quotes(200)
        # 确保获取到数据
        if df is None or len(df) == 0:
            return get_demo_data()
        return df
    except Exception as e:
        st.error(f"获取数据失败: {e}")
        return get_demo_data()


@st.cache_data(ttl=3600)
def get_stock_kline(symbol, adjust="qfq"):
    """获取K线数据"""
    if DEMO_MODE:
        # 生成模拟K线数据
        dates = pd.date_range(end=datetime.now(), periods=120, freq='D')
        base_price = np.random.uniform(10, 50)
        prices = base_price + np.cumsum(np.random.randn(120) * 0.5)
        
        df = pd.DataFrame({
            '日期': dates,
            '开盘': prices * (1 + np.random.uniform(-0.02, 0.02, 120)),
            '收盘': prices,
            '最高': prices * (1 + np.random.uniform(0, 0.05, 120)),
            '最低': prices * (1 - np.random.uniform(0, 0.05, 120)),
            '成交量': np.random.randint(1000000, 50000000, 120),
            '成交额': np.random.randint(10000000, 500000000, 120),
            '振幅': np.random.uniform(0, 5, 120),
            '涨跌幅': np.random.uniform(-5, 5, 120),
            '涨跌额': np.random.uniform(-2, 2, 120),
            '换手率': np.random.uniform(0, 10, 120),
        })
        return df
    
    try:
        # 使用腾讯财经数据源
        return tencent.get_stock_kline(symbol)
    except Exception as e:
        return None


@st.cache_data(ttl=3600)
def get_stock_info(symbol):
    """获取股票基本信息"""
    if DEMO_MODE:
        return {
            '代码': symbol,
            '名称': f'股票{symbol}',
            '最新价': round(np.random.uniform(10, 50), 2),
            '涨跌幅': round(np.random.uniform(-5, 5), 2),
        }
    
    try:
        # 使用腾讯财经数据源
        df = tencent.get_realtime_quote([symbol])
        if len(df) > 0:
            return df.iloc[0].to_dict()
        return {}
    except:
        return None
    try:
        return ak.stock_individual_info_em(symbol=symbol)
    except:
        return None


# ==================== 技术指标计算 ====================

def calculate_indicators(df):
    """计算技术指标"""
    result = df.copy()
    
    # 均线
    for period in [5, 10, 20, 60, 120]:
        result[f'MA{period}'] = result['收盘'].rolling(window=period).mean()
    
    # MACD
    result['EMA12'] = result['收盘'].ewm(span=12, adjust=False).mean()
    result['EMA26'] = result['收盘'].ewm(span=26, adjust=False).mean()
    result['DIF'] = result['EMA12'] - result['EMA26']
    result['DEA'] = result['DIF'].ewm(span=9, adjust=False).mean()
    result['MACD'] = (result['DIF'] - result['DEA']) * 2
    
    # KDJ
    low_low = result['最低'].rolling(window=9).min()
    high_high = result['最高'].rolling(window=9).max()
    result['RSV'] = (result['收盘'] - low_low) / (high_high - low_low) * 100
    result['K'] = result['RSV'].ewm(span=3, adjust=False).mean()
    result['D'] = result['K'].ewm(span=3, adjust=False).mean()
    result['J'] = 3 * result['K'] - 2 * result['D']
    
    # OBV
    result['OBV'] = (np.sign(result['收盘'].diff()) * result['成交量']).fillna(0).cumsum()
    
    return result


# ==================== K线图表 ====================

def plot_candlestick(df, symbol, name):
    """绘制K线图表"""
    
    # 转换日期格式
    df['日期'] = pd.to_datetime(df['日期'])
    
    # 计算均线
    df = calculate_indicators(df)
    
    # 创建图表
    fig = make_subplots(
        rows=4, cols=1,
        shared_xaxes=True,
        vertical_spacing=0.05,
        row_heights=[0.5, 0.15, 0.15, 0.15],
        subplot_titles=('K线 & 均线', '成交量', 'MACD', 'KDJ')
    )
    
    # K线
    fig.add_trace(go.Candlestick(
        x=df['日期'],
        open=df['开盘'],
        high=df['最高'],
        low=df['最低'],
        close=df['收盘'],
        name='K线'
    ), row=1, col=1)
    
    # 均线
    colors = {'MA5': '#ff6b6b', 'MA10': '#4ecdc4', 'MA20': '#45b7d1', 'MA60': '#96ceb4'}
    for period in [5, 10, 20, 60]:
        if f'MA{period}' in df.columns:
            fig.add_trace(go.Scatter(
                x=df['日期'], y=df[f'MA{period}'],
                mode='lines', name=f'MA{period}',
                line=dict(color=colors.get(f'MA{period}', 'gray'), width=1)
            ), row=1, col=1)
    
    # 成交量
    colors_vol = ['#ef4444' if df['收盘'].iloc[i] >= df['开盘'].iloc[i] else '#22c55e' 
                  for i in range(len(df))]
    fig.add_trace(go.Bar(
        x=df['日期'], y=df['成交量'],
        marker_color=colors_vol,
        name='成交量'
    ), row=2, col=1)
    
    # MACD
    fig.add_trace(go.Bar(
        x=df['日期'], y=df['MACD'],
        marker_color='#6366f1',
        name='MACD'
    ), row=3, col=1)
    fig.add_trace(go.Scatter(
        x=df['日期'], y=df['DIF'],
        mode='lines', name='DIF',
        line=dict(color='#f59e0b', width=1)
    ), row=3, col=1)
    fig.add_trace(go.Scatter(
        x=df['日期'], y=df['DEA'],
        mode='lines', name='DEA',
        line=dict(color='#8b5cf6', width=1)
    ), row=3, col=1)
    
    # KDJ
    fig.add_trace(go.Scatter(
        x=df['日期'], y=df['K'],
        mode='lines', name='K',
        line=dict(color='#f97316', width=1)
    ), row=4, col=1)
    fig.add_trace(go.Scatter(
        x=df['日期'], y=df['D'],
        mode='lines', name='D',
        line=dict(color='#06b6d4', width=1)
    ), row=4, col=1)
    fig.add_trace(go.Scatter(
        x=df['日期'], y=df['J'],
        mode='lines', name='J',
        line=dict(color='#ec4899', width=1)
    ), row=4, col=1)
    
    # 布局设置
    fig.update_layout(
        title=f'{symbol} {name} - 日K线',
        template='plotly_dark',
        height=800,
        showlegend=True,
        legend=dict(orientation="h", yanchor="bottom", y=1.02),
        xaxis_rangeslider_visible=False
    )
    
    return fig


# ==================== 侧边栏 ====================

st.sidebar.title("⚙️ 系统设置")

st.sidebar.header("📊 数据控制")
if st.sidebar.button("🔄 强制刷新数据"):
    st.rerun()

st.sidebar.header("🎯 选股条件")
min_change = st.sidebar.slider("最小涨幅(%)", -10, 10, 2, 1)
min_volume = st.sidebar.number_input("最小成交额(亿)", 0.0, 100.0, 0.5, 0.5)

st.sidebar.header("📈 技术筛选")
use_ma_filter = st.sidebar.checkbox("均线多头排列", value=True)
use_macd_filter = st.sidebar.checkbox("MACD金叉", value=False)
use_kdj_filter = st.sidebar.checkbox("KDJ金叉", False)

# ==================== 主页面 ====================

st.markdown('<p class="title">🏆 A股全能量化选股系统</p>', unsafe_allow_html=True)
st.markdown('<p class="subtitle">融合缠论结构 · CPV量价分析 · 基本面筛选</p>', unsafe_allow_html=True)

if DEMO_MODE:
    st.warning("⚠️ 当前为演示模式，使用模拟数据")

# 获取数据
with st.spinner("📥 正在获取A股实时数据..."):
    df = get_realtime_quotes()

if df is not None:
    # 市场概览
    st.subheader("📊 市场概览")
    
    col1, col2, col3, col4 = st.columns(4)
    
    total_stocks = len(df)
    up_stocks = len(df[df['涨跌幅'] > 0])
    down_stocks = len(df[df['涨跌幅'] < 0])
    avg_change = df['涨跌幅'].mean()
    
    with col1:
        st.metric("A股总数", f"{total_stocks}")
    with col2:
        st.metric("上涨", f"{up_stocks} ↑", f"{up_stocks/total_stocks*100:.1f}%")
    with col3:
        st.metric("下跌", f"{down_stocks} ↓", f"-{down_stocks/total_stocks*100:.1f}%")
    with col4:
        st.metric("平均涨幅", f"{avg_change:.2f}%")
    
    # 选股筛选
    st.subheader("🎯 选股结果")
    
    # 显示数据来源
    if not DEMO_MODE:
        st.caption(f"📡 数据来源: 腾讯财经 | 股票数: {len(df)}")
    
    # 应用筛选条件
    filtered = df.copy()
    
    # 涨幅筛选
    filtered = filtered[filtered['涨跌幅'] >= min_change]
    
    # 成交额筛选 (转换为亿)
    if '成交额' in filtered.columns:
        filtered['成交额_亿'] = filtered['成交额'] / 100000000
        filtered = filtered[filtered['成交额_亿'] >= min_volume]
    
    st.write(f"筛选条件: 涨幅 >= {min_change}% 且 成交额 >= {min_volume}亿")
    st.write(f"根据条件筛选出 **{len(filtered)}** 只股票")
    
    # 显示结果表格
    if len(filtered) > 0:
        # 选择显示列
        display_cols = ['代码', '名称', '最新价', '涨跌幅', '成交额_亿' if '成交额_亿' in filtered.columns else '成交量', '换手率']
        display_cols = [c for c in display_cols if c in filtered.columns]
        
        # 排序
        filtered = filtered.sort_values('涨跌幅', ascending=False)
        
        # 分页显示
        page_size = 20
        total_pages = (len(filtered) + page_size - 1) // page_size
        page = st.number_input(f"页码 (共{total_pages}页)", 1, total_pages, 1)
        
        start_idx = (page - 1) * page_size
        end_idx = min(start_idx + page_size, len(filtered))
        
        st.dataframe(
            filtered[display_cols].iloc[start_idx:end_idx],
            use_container_width=True,
            height=400
        )
        
        # 股票详情分析
        st.subheader("📈 个股详细分析")
        
        col1, col2 = st.columns([1, 3])
        
        with col1:
            selected_symbol = st.text_input("输入股票代码", "000001")
            selected_symbol = selected_symbol.zfill(6)
            
            # 获取股票名称
            stock_info = filtered[filtered['代码'] == selected_symbol]
            if len(stock_info) > 0:
                stock_name = stock_info['名称'].iloc[0]
            else:
                stock_name = ""
            
            if st.button("🔍 分析该股票"):
                kline_data = get_stock_kline(selected_symbol)
                
                if kline_data is not None and len(kline_data) > 0:
                    st.session_state['kline_data'] = kline_data
                    st.session_state['symbol'] = selected_symbol
                    st.session_state['name'] = stock_name
        
        with col2:
            if 'kline_data' in st.session_state:
                fig = plot_candlestick(
                    st.session_state['kline_data'],
                    st.session_state['symbol'],
                    st.session_state['name']
                )
                st.plotly_chart(fig, use_container_width=True)
                
                # 技术指标解读
                kline = st.session_state['kline_data']
                kline = calculate_indicators(kline)
                
                st.subheader("📊 技术指标解读")
                
                col_a, col_b, col_c, col_d = st.columns(4)
                
                with col_a:
                    ma_status = "多头↑" if kline['MA20'].iloc[-1] > kline['MA60'].iloc[-1] else "空头↓"
                    st.metric("均线状态", ma_status)
                
                with col_b:
                    macd_status = "金叉↑" if kline['MACD'].iloc[-1] > 0 else "死叉↓"
                    st.metric("MACD", macd_status)
                
                with col_c:
                    kdj_status = "超买" if kline['J'].iloc[-1] > 100 else "超卖" if kline['J'].iloc[-1] < 0 else "正常"
                    st.metric("KDJ", kdj_status)
                
                with col_d:
                    obv_trend = "上涨↑" if kline['OBV'].iloc[-1] > kline['OBV'].iloc[-20] else "下跌↓"
                    st.metric("OBV", obv_trend)

else:
    st.error("❌ 无法获取A股数据，请检查网络连接")
    
    st.info("""
    💡 解决方案:
    1. 检查网络是否正常访问国内网站
    2. 如果使用代理软件，请确保代理正常工作
    3. 或者暂时关闭代理尝试
    """)

# 页脚
st.markdown("---")
st.markdown(
    "<div style='text-align: center; color: #6b7280;'>"
    "📈 A股量化选股系统 | 仅供个人学习研究，不构成投资建议<br>"
    "© 2026 A-Quant System"
    "</div>",
    unsafe_allow_html=True
)

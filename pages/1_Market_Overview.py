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

# 演示模式 - 设置为False使用真实数据
DEMO_MODE = False

# 导入自己的数据模块
from data_source import EastMoneyData
from selector import ComprehensiveSelector
from sector_analysis import SectorAnalysis
from tencent_source import TencentDataSource
from user_config import get_user_config
from deepseek_analyzer import get_deepseek_analyzer
from pdf_generator_professional import generate_professional_pdf_report
from wechat_sender import get_wechat_sender
from market_analyzer import get_market_analyzer
from web_designer import get_web_designer
from stock_code_lookup import get_stock_code_lookup

# 初始化数据对象
em = EastMoneyData()
selector = ComprehensiveSelector()
sector_analysis = SectorAnalysis()
tencent = TencentDataSource()

# 初始化用户配置
user_config = get_user_config()
filter_conditions = user_config.get_filter_conditions()

# 页面配置
st.set_page_config(
    page_title="A股量化选股系统",
    page_icon="📈",
    layout="wide",
    initial_sidebar_state="expanded"
)

# 样式设置 - 专业报告风格  
# 使用web_designer模块生成的专业报告样式
designer = get_web_designer()
professional_css = designer.generate_professional_report_css()
st.markdown(professional_css, unsafe_allow_html=True)

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


@st.cache_data(ttl=60, show_spinner=False)  # 缩短缓存时间为1分钟
def get_realtime_quotes():
    """获取实时行情"""
    if DEMO_MODE:
        return get_demo_data()
    
    try:
        # 使用腾讯财经数据源
        df = tencent.get_realtime_quotes(5000)
        # 确保获取到数据
        if df is None or len(df) == 0:
            return get_demo_data()
        return df
    except Exception as e:
        st.error(f"获取数据失败: {e}")
        return get_demo_data()


@st.cache_data(ttl=60, show_spinner=False)  # 缓存1分钟，确保数据更新
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
        kline_data = tencent.get_stock_kline(symbol)
        # 验证数据有效性
        if kline_data is not None and len(kline_data) > 0:
            # 检查数据是否为最新（基于日期）
            latest_date = kline_data.index.max() if kline_data.index.name == '日期' else None
            if latest_date:
                # 检查是否是今天的数据
                today = pd.Timestamp.today().normalize()
                if latest_date < today:
                    # 数据不是最新的，尝试使用东方财富数据源
                    from data_source import EastMoneyData
                    em = EastMoneyData()
                    em_kline = em.get_stock_kline(symbol)
                    if em_kline is not None and len(em_kline) > 0:
                        return em_kline
            return kline_data
        else:
            # 腾讯数据源失败，尝试东方财富数据源
            from data_source import EastMoneyData
            em = EastMoneyData()
            em_kline = em.get_stock_kline(symbol)
            if em_kline is not None and len(em_kline) > 0:
                return em_kline
            return None
    except Exception as e:
        # 异常情况下尝试东方财富数据源
        try:
            from data_source import EastMoneyData
            em = EastMoneyData()
            em_kline = em.get_stock_kline(symbol)
            if em_kline is not None and len(em_kline) > 0:
                return em_kline
        except:
            pass
        return None


@st.cache_data(ttl=60, show_spinner=False)  # 缓存1分钟
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
        
        # 腾讯数据源失败，尝试东方财富数据源
        from data_source import EastMoneyData
        em = EastMoneyData()
        quote = em.get_realtime_quote(symbol)
        if quote and '代码' in quote:
            return quote
            
        return {}
    except Exception as e:
        print(f"获取股票信息失败: {e}")
        return None


# ==================== 技术指标计算 ====================

from indicators import calculate_all_indicators, get_technical_status, calculate_cpv


def calculate_indicators(df):
    """计算技术指标"""
    return calculate_all_indicators(df)


# ==================== K线图表 ====================

def plot_candlestick(df, symbol, name):
    """绘制K线图表"""
    
    # 复制数据以避免修改原始数据
    df = df.copy()
    
    # 处理日期索引
    if '日期' not in df.columns:
        # 如果日期是索引，重置索引并将索引转换为日期列
        if df.index.name == '日期':
            df.reset_index(inplace=True)
        else:
            # 如果索引不是日期，尝试使用索引作为日期
            df['日期'] = df.index
    
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
    if 'MACD' in df.columns and 'DIF' in df.columns and 'DEA' in df.columns:
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
    if 'K' in df.columns and 'D' in df.columns and 'J' in df.columns:
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
        height=500,
        showlegend=True,
        legend=dict(orientation="h", yanchor="bottom", y=1.02),
        xaxis_rangeslider_visible=False
    )
    
    return fig


# ==================== 侧边栏 ====================
# 侧边栏
st.sidebar.title("⚙️ 系统设置")

st.sidebar.header("📊 数据控制")
if st.sidebar.button("🔄 强制刷新数据"):
    st.rerun()

# 演示模式切换
st.sidebar.header("🎮 运行模式")
demo_mode = st.sidebar.checkbox("演示模式", value=DEMO_MODE)
if demo_mode != DEMO_MODE:
    DEMO_MODE = demo_mode
    st.sidebar.info("运行模式已切换，正在重启...")
    st.rerun()

st.sidebar.header("🤖 AI分析设置")

# API密钥查看密码保护
api_key_visible = False
password_input = st.sidebar.text_input("输入密码查看API密钥", type="password")

if st.sidebar.button("🔓 解锁API密钥"):
    # 简单的密码验证（实际应用中应该使用更安全的验证方式）
    if password_input == "admin123":  # 这里可以修改为更安全的密码
        api_key_visible = True
        st.sidebar.success("✅ 密码正确，API密钥已解锁")
    else:
        st.sidebar.error("❌ 密码错误，请重试")

# 根据解锁状态显示API密钥输入框
if api_key_visible:
    deepseek_api_key = st.sidebar.text_input("DeepSeek API密钥", value=user_config.get_deepseek_api_key())
    
    if st.sidebar.button("💾 保存API密钥"):
        user_config.set_deepseek_api_key(deepseek_api_key)
        st.sidebar.success("API密钥保存成功！")
else:
    st.sidebar.info("� API密钥已锁定，请输入密码解锁")
    # 未解锁时不显示API密钥输入框
    deepseek_api_key = None

# AI状态指示器
st.sidebar.header("🤖 AI状态")
if user_config.get_deepseek_api_key():
    st.sidebar.success("✅ AI分析功能可用")
    
    # 添加API连接测试按钮
    if st.sidebar.button("🔧 测试API连接"):
        with st.spinner("正在测试DeepSeek API连接..."):
            try:
                from deepseek_analyzer import get_deepseek_analyzer
                analyzer = get_deepseek_analyzer(user_config.get_deepseek_api_key())
                # 测试连接
                test_result = analyzer.test_connection()
                if test_result:
                    st.sidebar.success("✅ API连接测试成功！")
                    st.sidebar.info(f"连接状态: {test_result}")
                else:
                    st.sidebar.error("❌ API连接测试失败")
            except Exception as e:
                st.sidebar.error(f"❌ API连接测试失败: {e}")
else:
    st.sidebar.warning("⚠️ AI分析功能未设置")
    st.sidebar.info("请输入DeepSeek API密钥以启用AI分析")

# 企业微信配置
st.sidebar.header("💬 企业微信设置")
wechat_config = user_config.get_wechat_config()
corpid = st.sidebar.text_input("企业ID (corpid)", value=wechat_config.get('corpid', ''))
corpsecret = st.sidebar.text_input("应用密钥 (corpsecret)", type="password", value=wechat_config.get('corpsecret', ''))
agentid = st.sidebar.text_input("应用ID (agentid)", value=wechat_config.get('agentid', ''))
user_id = st.sidebar.text_input("接收用户ID", value=wechat_config.get('user_id', ''))

if st.sidebar.button("💾 保存企业微信配置"):
    new_wechat_config = {
        'corpid': corpid,
        'corpsecret': corpsecret,
        'agentid': agentid,
        'user_id': user_id
    }
    user_config.set_wechat_config(new_wechat_config)
    st.sidebar.success("企业微信配置保存成功！")

# 企业微信状态
if all([wechat_config.get('corpid'), wechat_config.get('corpsecret'), wechat_config.get('agentid')]):
    st.sidebar.success("✅ 企业微信配置完整")
else:
    st.sidebar.warning("⚠️ 企业微信配置不完整")
    st.sidebar.info("请填写完整的企业微信配置以启用PDF发送功能")

# 智能筛选条件设置
with st.sidebar.expander("🎯 智能筛选条件", expanded=True):
    # 核心筛选条件
    st.markdown("### 📊 核心筛选")
    min_change = st.slider("最小涨幅(%)", -10, 10, filter_conditions.get('min_change', 2), 1)
    min_volume = st.number_input("最小成交额(亿)", 0.0, 100.0, filter_conditions.get('min_volume', 0.5), 0.5)
    
    # 技术指标筛选
    st.markdown("### 📈 技术指标")
    use_ma_filter = st.checkbox("均线多头排列", value=filter_conditions.get('use_ma_filter', True))
    use_macd_filter = st.checkbox("MACD金叉", value=filter_conditions.get('use_macd_filter', False))
    use_kdj_filter = st.checkbox("KDJ金叉", value=filter_conditions.get('use_kdj_filter', False))
    use_cpv_filter = st.checkbox("CPV量价配合", value=filter_conditions.get('use_cpv_filter', True))
    
    # 基本面筛选
    st.markdown("### 💰 基本面")
    max_pe = st.slider("最大市盈率", 0, 200, filter_conditions.get('max_pe', 50), 5)
    min_roe = st.slider("最小ROE (%)", 0, 50, filter_conditions.get('min_roe', 5), 1)
    
    # 保存用户配置
    if st.button("💾 保存配置"):
        new_conditions = {
            'min_change': min_change,
            'min_volume': min_volume,
            'use_ma_filter': use_ma_filter,
            'use_macd_filter': use_macd_filter,
            'use_kdj_filter': use_kdj_filter,
            'use_cpv_filter': use_cpv_filter,
            'max_pe': max_pe,
            'min_roe': min_roe,
        }
        user_config.set_filter_conditions(new_conditions)
        st.success("配置保存成功!")

# 智能选股提示
st.sidebar.markdown("---")
st.sidebar.info("💡 **智能选股提示**\n\n我们的系统融合了：\n- 📈 缠论结构分析\n- 📊 CPV量价分析\n- 💰 基本面筛选\n- 🔥 板块效应\n\n系统会自动为股票计算综合评分，\n并按评分高低排序展示！")

# ==================== 主页面 ====================

# 专业页眉设计 - 简化版
# 使用更简单的HTML结构，避免复杂的CSS
current_time = datetime.now().strftime('%Y-%m-%d %H:%M:%S')

# 使用Streamlit的原生布局功能
col1, col2 = st.columns([3, 1])

with col1:
    st.markdown("# 📊 个人选股系统")
    st.markdown("⚠️ 不构成投资建议")
    st.markdown("📧 作者邮箱：chaowang687@gmail.com")
    st.markdown("融合缠论结构 · CPV量价分析 · 基本面筛选 · AI智能分析")

with col2:
    st.markdown(f"**🕐 {current_time}**")

# 添加分隔线
st.markdown("---")

if DEMO_MODE:
    st.warning("⚠️ 当前为演示模式，使用模拟数据")

# 创建标签页
# 创建动态加载动画
loading_placeholder = st.empty()

# 动态加载文本
loading_texts = [
    "📥 正在获取A股实时数据...",
    "📊 正在处理市场数据...",
    "⚡ 正在准备市场概览..."
]

try:
    # 阶段1: 获取数据
    loading_placeholder.info(loading_texts[0])
    import time
    start_time = time.time()
    
    df = get_realtime_quotes()
    
    # 阶段2: 处理数据
    loading_placeholder.info(loading_texts[1])
    
    # 阶段3: 准备概览
    loading_placeholder.info(loading_texts[2])
    
    # 确保加载动画至少显示1秒
    if time.time() - start_time < 1:
        time.sleep(1 - (time.time() - start_time))
finally:
    # 清除加载动画
    loading_placeholder.empty()
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
    
    # 应用智能筛选
    filtered = df.copy()
    
    # 核心筛选条件
    filtered = filtered[
        (filtered['涨跌幅'] >= min_change) &
        (filtered['成交额'] >= min_volume * 100000000)  # 转换为元
    ].copy()
    
    # 计算成交额(亿)用于显示
    if '成交额' in filtered.columns:
        filtered['成交额_亿'] = filtered['成交额'] / 100000000
    
    # 尝试计算综合评分 - 基于缠论结构、CPV量价分析和基本面筛选
    try:
        # 直接在筛选结果上计算综合评分
        if len(filtered) > 0:
            # 创建评分列
            filtered['综合得分'] = 0
            
            # 初始化选股器和相关工具
            selector = ComprehensiveSelector()
            
            # 计算每只股票的综合评分
            scores = []
            for index, row in filtered.iterrows():
                stock_code = row['代码']
                score = 0
                
                try:
                    # 1. 缠论结构分析 (30%权重)
                    chan_score = 0
                    chan_result = selector.analyze_stock_chanlun(stock_code)
                    if chan_result:
                        # 笔的方向和数量
                        if chan_result.get('笔数', 0) >= 3:
                            if chan_result.get('结构') == '上涨中枢':
                                chan_score += 15
                            elif chan_result.get('结构') == '下跌中枢':
                                chan_score += 5
                            else:
                                chan_score += 10
                        
                        # 中枢数量
                        chan_score += chan_result.get('中枢数', 0) * 5
                        
                        # 信号
                        signals = chan_result.get('信号', [])
                        if any('买入' in s for s in signals):
                            chan_score += 10
                        elif any('卖出' in s for s in signals):
                            chan_score -= 5
                    chan_score = min(30, max(0, chan_score))
                    
                    # 2. CPV量价分析 (25%权重)
                    cpv_score = 0
                    # 获取K线数据
                    kline_data = get_stock_kline(stock_code)
                    if kline_data is not None and len(kline_data) > 20:
                        # 计算CPV指标
                        cpv_data = calculate_cpv(kline_data)
                        # 量价配合情况
                        recent_cpv = cpv_data.tail(10)
                        positive_cpv = len(recent_cpv[recent_cpv['CPV_SCORE'] > 0])
                        cpv_score = (positive_cpv / 10) * 25
                    cpv_score = min(25, max(0, cpv_score))
                    
                    # 3. 基本面分析 (25%权重)
                    fund_score = 0
                    # 尝试获取基本面数据
                    fund_data = selector.fs.get_stock_list_with_fundamental(500)
                    stock_fund = fund_data[fund_data['代码'] == stock_code]
                    if not stock_fund.empty:
                        fund_info = stock_fund.iloc[0]
                        # 市盈率
                        pe = fund_info.get('市盈率', 0)
                        if 0 < pe < 30:
                            fund_score += 10
                        elif pe < 50:
                            fund_score += 5
                        
                        # 净资产收益率
                        roe = fund_info.get('净资产收益率', 0)
                        fund_score += min(10, roe)
                        
                        # 净利润增长
                        profit_growth = fund_info.get('净利润同比增长', 0)
                        if profit_growth > 20:
                            fund_score += 5
                    fund_score = min(25, max(0, fund_score))
                    
                    # 4. 技术指标分析 (20%权重)
                    tech_score = 0
                    if kline_data is not None and len(kline_data) > 60:
                        # 计算技术指标
                        tech_data = calculate_all_indicators(kline_data)
                        indicator_status = get_technical_status(tech_data)
                        
                        # 均线状态
                        if indicator_status.get('ma_status') == '多头↑':
                            tech_score += 8
                        
                        # MACD状态
                        if indicator_status.get('macd_status') == '金叉↑':
                            tech_score += 6
                        
                        # KDJ状态
                        if indicator_status.get('kdj_status') == '超卖':
                            tech_score += 3
                        elif indicator_status.get('kdj_status') == '超买':
                            tech_score -= 2
                        
                        # OBV状态
                        if indicator_status.get('obv_trend') == '上涨↑':
                            tech_score += 3
                    tech_score = min(20, max(0, tech_score))
                    
                    # 计算总得分
                    total_score = chan_score + cpv_score + fund_score + tech_score
                    scores.append(total_score)
                except Exception as e:
                    print(f"计算股票{stock_code}的综合评分时出错: {e}")
                    scores.append(0)
            
            # 添加得分到DataFrame
            filtered['综合得分'] = scores
            
            # 按综合得分排序
            filtered = filtered.sort_values('综合得分', ascending=False)
            
            # 打印调试信息
            print(f"计算综合评分成功，共{len(filtered)}只股票，最高评分:{filtered['综合得分'].max():.2f}")
    except Exception as e:
        print(f"计算综合评分时出错: {e}")
        # 如果出错，按涨跌幅排序
        if '涨跌幅' in filtered.columns:
            filtered = filtered.sort_values('涨跌幅', ascending=False)
        # 添加一个默认的综合得分列
        filtered['综合得分'] = filtered.get('涨跌幅', 0)
        print(f"使用涨跌幅作为默认综合得分")
    
    # 显示筛选结果信息
    st.markdown("### 🎯 智能筛选结果")
    st.markdown(f"**筛选条件:** 涨幅 >= {min_change}% 且 成交额 >= {min_volume}亿")
    st.markdown(f"**筛选结果:** 根据条件筛选出 **{len(filtered)}** 只股票")
    
    # 系统核心功能提示
    if len(filtered) > 0:
        st.info("💡 **系统核心功能**\n\n我们的系统融合了：\n- 📈 缠论结构分析\n- 📊 CPV量价分析\n- 💰 基本面筛选\n- 🔥 板块效应\n\n股票已按综合评分高低排序，评分越高的股票综合表现越好！")
    
    # 显示结果表格
    if len(filtered) > 0:
        # 选择显示列，确保包含综合得分
        display_cols = ['代码', '名称', '最新价', '涨跌幅', '成交额_亿' if '成交额_亿' in filtered.columns else '成交量', '换手率']
        if '综合得分' in filtered.columns:
            display_cols.insert(2, '综合得分')  # 将综合得分插入到名称后面
        display_cols = [c for c in display_cols if c in filtered.columns]
        
        # 分页显示
        page_size = 20
        total_pages = (len(filtered) + page_size - 1) // page_size
        page = st.number_input(f"页码 (共{total_pages}页)", 1, total_pages, 1)
        
        start_idx = (page - 1) * page_size
        end_idx = min(start_idx + page_size, len(filtered))
        
        # 显示数据表格
        st.dataframe(
            filtered[display_cols].iloc[start_idx:end_idx],
            use_container_width=True,
            height=400,
            column_config={
                "代码": st.column_config.TextColumn(
                    "代码",
                    width="small"
                ),
                "名称": st.column_config.TextColumn(
                    "名称",
                    width="small"
                ),
                "综合得分": st.column_config.NumberColumn(
                    "综合得分",
                    width="small",
                    format="%.2f"
                ) if '综合得分' in filtered.columns else None,
                "最新价": st.column_config.NumberColumn(
                    "最新价",
                    width="small",
                    format="%.2f"
                ),
                "涨跌幅": st.column_config.NumberColumn(
                    "涨跌幅(%)",
                    width="small",
                    format="%.2f"
                ),
                "成交额_亿": st.column_config.NumberColumn(
                    "成交额(亿)",
                    width="small",
                    format="%.2f"
                ) if '成交额_亿' in filtered.columns else None,
                "成交量": st.column_config.NumberColumn(
                    "成交量",
                    width="small"
                ) if '成交量' in filtered.columns and '成交额_亿' not in filtered.columns else None,
                "换手率": st.column_config.NumberColumn(
                    "换手率(%)",
                    width="small",
                    format="%.2f"
                )
            }
        )
        
        # 顶部股票提示
        if '综合得分' in filtered.columns:
            top_stock = filtered.iloc[0]
            st.success(f"🏆 **评分最高的股票**：{top_stock['名称']} ({top_stock['代码']})，综合评分：{top_stock['综合得分']:.2f}")
        else:
            top_stock = filtered.iloc[0]
            st.success(f"🏆 **涨幅最高的股票**：{top_stock['名称']} ({top_stock['代码']})，涨幅：{top_stock['涨跌幅']:.2f}%")
        
        # 股票详情分析
        st.subheader("📈 个股详细分析")
        
        col1, col2 = st.columns([1, 3])
        
        with col1:
            # 最近访问的股票
            recent_stocks = user_config.get_recent_stocks()
            if recent_stocks:
                st.subheader("最近访问")
                for stock_code, stock_name in recent_stocks:
                    if st.button(f"{stock_code} {stock_name}", key=f"recent_{stock_code}"):
                        selected_symbol = stock_code
                        kline_data = get_stock_kline(selected_symbol)
                        
                        if kline_data is not None and len(kline_data) > 0:
                            st.session_state['kline_data'] = kline_data
                            st.session_state['symbol'] = selected_symbol
                            st.session_state['name'] = stock_name
                            st.rerun()
                st.markdown("---")
            
            # 股票代码查询功能
            st.subheader("🔍 股票代码查询")
            
            # 股票名称查询
            stock_name_input = st.text_input("输入股票名称", "")
            
            if stock_name_input:
                # 初始化股票代码查询器
                stock_lookup = get_stock_code_lookup()
                
                # 查询股票代码
                with st.spinner(f"正在查询{stock_name_input}的股票代码..."):
                    results = stock_lookup.lookup_by_name(stock_name_input)
                
                if results:
                    st.success(f"找到{len(results)}个匹配结果")
                    
                    # 显示查询结果
                    for i, result in enumerate(results, 1):
                        stock_name = result['name']
                        stock_code = result['code']
                        market = result['market']
                        source = result['source']
                        
                        if st.button(f"{i}. {stock_name} ({stock_code}) - 来源: {source}", key=f"lookup_{i}_{stock_code}"):
                            selected_symbol = stock_code
                            st.session_state['selected_symbol'] = selected_symbol
                            st.session_state['selected_name'] = stock_name
                            st.rerun()
                else:
                    st.error(f"未找到{stock_name_input}的股票代码")
            
            # 传统的股票代码输入
            st.subheader("📟 直接输入股票代码")
            selected_symbol = st.text_input("输入股票代码", "000001")
            selected_symbol = selected_symbol.zfill(6)
            
            # 获取股票名称
            stock_name = ""
            
            # 首先在筛选结果中查找
            stock_info = filtered[filtered['代码'] == selected_symbol]
            if len(stock_info) > 0:
                stock_name = stock_info['名称'].iloc[0]
            else:
                # 如果筛选结果中没有，使用get_stock_info函数获取
                info = get_stock_info(selected_symbol)
                if info and '名称' in info:
                    stock_name = info['名称']
            
            # 显示股票名称
            if stock_name:
                st.info(f"股票名称: {stock_name}")
            else:
                st.info("正在获取股票名称...")
            
            if st.button("🔍 分析该股票"):
                # 检查是否从股票代码查询功能中选择了股票
                if 'selected_symbol' in st.session_state and 'selected_name' in st.session_state:
                    selected_symbol = st.session_state['selected_symbol']
                    stock_name = st.session_state['selected_name']
                    del st.session_state['selected_symbol']
                    del st.session_state['selected_name']
                
                with st.spinner("📊 正在获取股票数据..."):
                    kline_data = get_stock_kline(selected_symbol)
                    
                    # 如果还没有股票名称，再次尝试获取
                    if not stock_name:
                        info = get_stock_info(selected_symbol)
                        if info and '名称' in info:
                            stock_name = info['名称']
                
                if kline_data is not None and len(kline_data) > 0:
                    # 添加到最近访问
                    user_config.add_recent_stock(selected_symbol, stock_name)
                    
                    st.session_state['kline_data'] = kline_data
                    st.session_state['symbol'] = selected_symbol
                    st.session_state['name'] = stock_name
                    st.rerun()
                else:
                    st.error("❌ 获取股票数据失败")
                    st.info("💡 可能的原因：\n1. 股票代码不存在\n2. 网络连接问题\n3. 数据源暂时不可用\n\n建议：尝试使用演示模式或检查网络连接")
        
        with col2:
            if 'kline_data' in st.session_state:
                # 显示股票基本信息
                st.markdown(f"### 📈 **{st.session_state['name']} ({st.session_state['symbol']})**")
                
                # ====== AI分析功能 (主要分析方式) ======
                st.subheader("🤖 AI智能分析")
                
                # 获取DeepSeek API密钥
                deepseek_api_key = user_config.get_deepseek_api_key()
                
                if deepseek_api_key:
                    # 自动开始AI分析
                    # 创建动态加载动画
                    loading_placeholder = st.empty()
                        
                    # 动态加载文本
                    loading_texts = [
                        "🤔 AI正在分析股票数据...",
                        "📊 正在分析K线形态...",
                        "📈 正在评估市场趋势...",
                        "💰 正在分析资金流向...",
                        "⚡ 正在生成分析报告..."
                    ]
                    
                    # 准备分析数据
                    stock_data = {
                        'symbol': st.session_state['symbol'],
                        'name': st.session_state['name'],
                        'kline_data': st.session_state['kline_data']
                    }
                    
                    # 获取市场信息分析
                    try:
                        # 阶段1: 初始化分析器
                        loading_placeholder.info(loading_texts[0])
                        import time
                        start_time = time.time()
                        
                        market_analyzer = get_market_analyzer()
                        
                        # 阶段2: 分析K线形态
                        loading_placeholder.info(loading_texts[1])
                        
                        # 阶段3: 评估市场趋势
                        loading_placeholder.info(loading_texts[2])
                        
                        # 阶段4: 分析资金流向
                        loading_placeholder.info(loading_texts[3])
                        print(f"开始获取市场信息分析: {st.session_state['symbol']} - {st.session_state['name']}")
                        market_analysis = market_analyzer.comprehensive_analysis(
                            st.session_state['symbol'], 
                            st.session_state['name']
                        )
                        print(f"获取到市场信息分析: {market_analysis}")
                        stock_data['market_analysis'] = market_analysis
                        
                        # 计算实际分析时间
                        analysis_time = time.time() - start_time
                        
                        # 阶段5: 生成分析报告
                        loading_placeholder.info(loading_texts[4])
                        
                        # 确保每个阶段至少显示0.3秒，提供流畅的用户体验
                        if analysis_time < 2:
                            import time
                            time.sleep(2 - analysis_time)
                    except Exception as e:
                        print(f"获取市场信息失败: {e}")
                        # 即使市场信息获取失败，也继续分析
                        stock_data['market_analysis'] = {}
                        # 失败时显示错误信息1秒
                        import time
                        time.sleep(1)
                    
                    # 调用DeepSeek分析器
                    loading_placeholder.info("🤖 AI正在进行深度分析...")
                    analyzer = get_deepseek_analyzer(deepseek_api_key)
                    analysis_result = analyzer.analyze_stock(stock_data)
                    
                    # 清除加载动画
                    loading_placeholder.empty()
                    
                    # 显示分析结果
                    if analysis_result:
                        # 检查是否是错误信息
                        if "⚠️" in analysis_result or "错误" in analysis_result or "失败" in analysis_result or "网络连接" in analysis_result:
                            st.error("❌ AI分析失败")
                            st.markdown("### 📊 错误信息")
                            st.markdown(analysis_result)
                        else:
                            st.success("✅ AI分析完成！")
                            st.markdown("### 📊 AI分析结果")
                            st.markdown(analysis_result)
                    else:
                        st.error("❌ AI分析失败，请重试")
                    
                    # 显示市场信息分析
                    st.divider()
                    st.subheader("📰 市场信息分析")
                    
                    if 'market_analysis' in stock_data:
                        market_analysis = stock_data['market_analysis']
                        
                        # 显示利好利空因素
                        st.markdown("#### 📊 利好利空分析")
                        factors = market_analysis.get('factors', {})
                        
                        # 简化布局，避免复杂的列嵌套
                        bullish = factors.get('bullish', [])
                        bearish = factors.get('bearish', [])
                        
                        if bullish:
                            st.success("### 🟢 利好因素")
                            for factor in bullish[:5]:  # 显示前5条
                                st.markdown(f"- {factor}")
                        else:
                            st.info("暂无明显利好因素")
                        
                        if bearish:
                            st.error("### 🔴 利空因素")
                            for factor in bearish[:5]:  # 显示前5条
                                st.markdown(f"- {factor}")
                        else:
                            st.info("暂无明显利空因素")
                        
                        # 显示行业热点
                        st.markdown("#### 🔥 行业热点")
                        industry_hotspots = factors.get('industry_hotspots', [])
                        if industry_hotspots:
                            st.markdown("### 📈 行业热点信息")
                            for hotspot in industry_hotspots[:3]:  # 显示前3条
                                st.markdown(f"- {hotspot}")
                        else:
                            st.info("暂无行业热点信息")
                        
                        # 显示市场趋势
                        st.markdown("#### 📉 市场趋势")
                        market_trends = factors.get('market_trends', [])
                        if market_trends:
                            st.markdown("### 📊 市场趋势分析")
                            for trend in market_trends:
                                st.markdown(f"- {trend}")
                        else:
                            st.info("暂无市场趋势信息")
                        
                        # 显示主力资金状态
                        st.markdown("#### 💰 主力资金状态")
                        main_funds = market_analysis.get('main_funds', {})
                        
                        # 打印调试信息
                        print(f"market_analysis keys: {list(market_analysis.keys())}")
                        print(f"main_funds: {main_funds}")
                        print(f"main_funds type: {type(main_funds)}")
                        
                        if main_funds:
                            net_inflow = main_funds.get('net_inflow', 0)
                            status = main_funds.get('status', 'unknown')
                            
                            # 直接显示指标，避免列布局
                            st.metric("主力资金净流入", f"{net_inflow/10000:.2f}万")
                            
                            status_text = {
                                'inflow': '📈 流入',
                                'outflow': '📉 流出',
                                'balanced': '⚖️ 平衡'
                            }.get(status, '未知')
                            st.metric("资金状态", status_text)
                            
                            # 显示每日资金流向
                            daily_data = main_funds.get('daily_data', [])
                            
                            # 打印调试信息
                            print(f"daily_data: {daily_data}")
                            print(f"daily_data length: {len(daily_data)}")
                            
                            if daily_data:
                                st.markdown("##### 📅 近5日资金流向")
                                fund_df = pd.DataFrame(daily_data)
                                fund_df['net'] = fund_df['net'] / 10000  # 转换为万
                                fund_df = fund_df[['date', 'net']]
                                fund_df.columns = ['日期', '净流入(万)']
                                st.dataframe(fund_df, use_container_width=True)
                                
                                # 显示资金流向折线图
                                st.markdown("##### 📊 资金流向趋势")
                                import plotly.graph_objects as go
                                
                                # 准备数据
                                trend_df = pd.DataFrame(daily_data)
                                trend_df['date'] = pd.to_datetime(trend_df['date'])
                                trend_df = trend_df.sort_values('date')
                                
                                # 确保必要的列存在，如果不存在则创建
                                if 'main_net' not in trend_df.columns:
                                    trend_df['main_net'] = trend_df.get('net', 0)
                                if 'hot_money_net' not in trend_df.columns:
                                    trend_df['hot_money_net'] = trend_df.get('main_net', 0)
                                if 'retail_net' not in trend_df.columns:
                                    trend_df['retail_net'] = trend_df.get('main_net', 0) * 0.5
                                
                                # 转换为万
                                trend_df['main_net'] = trend_df['main_net'].fillna(0) / 10000
                                trend_df['hot_money_net'] = trend_df['hot_money_net'].fillna(0) / 10000
                                trend_df['retail_net'] = trend_df['retail_net'].fillna(0) / 10000
                                
                                # 打印调试信息
                                print("资金流向数据行数:", len(trend_df))
                                print("数据列:", list(trend_df.columns))
                                
                                # 简化图表创建，使用Streamlit的line_chart函数
                                try:
                                    print("创建资金流向折线图...")
                                    # 创建一个副本以避免修改原始数据
                                    chart_df = trend_df.copy()
                                    # 使用Streamlit的line_chart函数，这是一个更简单的方式
                                    # 首先设置日期为索引
                                    chart_df = chart_df.set_index('date')
                                    # 选择要显示的列
                                    chart_data = chart_df[['main_net', 'hot_money_net', 'retail_net']]
                                    # 显示图表，使用新的width参数
                                    st.line_chart(chart_data, width='stretch')
                                    print("资金流向折线图创建成功")
                                    print(f"图表数据形状: {chart_data.shape}")
                                    print(f"图表数据前5行:\n{chart_data.head()}")
                                except Exception as e:
                                    print("创建资金流向图表时出错:", e)
                                    # 显示错误信息
                                    st.error(f"创建资金流向图表时出错: {e}")
                                    # 尝试使用更简单的方式，不设置索引
                                    try:
                                        st.line_chart(trend_df[['date', 'main_net', 'hot_money_net', 'retail_net']], width='stretch')
                                    except Exception as e2:
                                        print("尝试更简单方式时出错:", e2)
                                        st.error(f"尝试更简单方式时出错: {e2}")
                                        # 最后尝试只显示main_net
                                        st.line_chart(trend_df[['date', 'main_net']], width='stretch')
                            
                            # 显示财务数据 - 近一年净利润变化
                            st.markdown("#### 📊 财务数据")
                            financial_data = market_analysis.get('financial_data', {})
                            
                            # 打印调试信息
                            print(f"financial_data: {financial_data}")
                            
                            if financial_data:
                                net_profit = financial_data.get('net_profit', [])
                                quarters = financial_data.get('quarters', [])
                                revenue = financial_data.get('revenue', [])
                                
                                if net_profit and quarters:
                                    st.markdown("##### 近一年净利润变化")
                                    
                                    # 准备净利润数据
                                    profit_df = pd.DataFrame({
                                        '季度': quarters,
                                        '净利润(亿元)': [np / 100000000 for np in net_profit]  # 转换为亿元
                                    })
                                    
                                    # 显示净利润数据表格
                                    st.dataframe(profit_df, use_container_width=True)
                                    
                                    # 创建净利润变化折线图
                                    try:
                                        print("创建净利润变化折线图...")
                                        # 设置季度为索引
                                        profit_df = profit_df.set_index('季度')
                                        # 显示图表
                                        st.line_chart(profit_df, width='stretch')
                                        print("净利润变化折线图创建成功")
                                        print(f"图表数据形状: {profit_df.shape}")
                                        print(f"图表数据:\n{profit_df}")
                                    except Exception as e:
                                        print("创建净利润变化图表时出错:", e)
                                        # 显示错误信息
                                        st.error(f"创建净利润变化图表时出错: {e}")
                                else:
                                    st.info("暂无净利润数据")
                            else:
                                st.info("暂无财务数据")
                        else:
                            st.info("暂无主力资金数据")
                        
                        # 显示市场环境
                        st.markdown("#### 📈 市场环境")
                        market_context = market_analysis.get('market_context', {})
                        
                        col_x, col_y = st.columns(2)
                        
                        with col_x:
                            industry_trend = market_context.get('industry_trend', 'unknown')
                            trend_text = {
                                'up': '📈 上涨',
                                'down': '📉 下跌',
                                'stable': '⚖️ 稳定',
                                'unknown': '❓ 未知'
                            }.get(industry_trend, '❓ 未知')
                            st.metric("行业趋势", trend_text)
                        
                        with col_y:
                            market_trend = market_context.get('market_trend', 'unknown')
                            market_text = {
                                'up': '📈 上涨',
                                'down': '📉 下跌',
                                'stable': '⚖️ 稳定',
                                'unknown': '❓ 未知'
                            }.get(market_trend, '❓ 未知')
                            st.metric("大盘趋势", market_text)
                    
                    # ====== 生成PDF报告并发送 =====
                    st.divider()
                    st.subheader("📄 PDF报告生成")
                    
                    # 生成PDF报告
                    with st.spinner("📊 正在生成PDF报告..."):
                        # 确保analysis_result有值
                        if not analysis_result:
                            analysis_result = "AI分析暂时不可用，以下是股票数据摘要"
                        
                        pdf_path = generate_professional_pdf_report(stock_data, analysis_result)
                        
                        if pdf_path:
                            st.success(f"✅ PDF报告生成成功: {pdf_path}")
                            
                            # 添加PDF报告下载功能
                            with open(pdf_path, "rb") as f:
                                pdf_data = f.read()
                            
                            download_filename = f"{stock_data['name']}_{stock_data['symbol']}_分析报告_{datetime.now().strftime('%Y%m%d_%H%M%S')}.pdf"
                            
                            st.download_button(
                                label="📥 下载PDF报告",
                                data=pdf_data,
                                file_name=download_filename,
                                mime="application/pdf"
                            )
                            
                            # 尝试发送到企业微信
                            wechat_config = user_config.get_wechat_config()
                            if all([wechat_config.get('corpid'), wechat_config.get('corpsecret'), 
                                    wechat_config.get('agentid'), wechat_config.get('user_id')]):
                                with st.spinner("💬 正在发送到企业微信..."):
                                    sender = get_wechat_sender(
                                        wechat_config.get('corpid'),
                                        wechat_config.get('corpsecret'),
                                        wechat_config.get('agentid')
                                    )
                                    success = sender.send_file_to_user(
                                        wechat_config.get('user_id'),
                                        pdf_path,
                                        f"{stock_data['name']} ({stock_data['symbol']}) 股票分析报告"
                                    )
                                    
                                    if success:
                                        st.success("✅ 企业微信发送成功！")
                                    else:
                                        st.warning("⚠️ 企业微信发送失败，请检查配置")
                            else:
                                st.info("💡 企业微信配置不完整，跳过发送")
                        else:
                            st.error("❌ PDF报告生成失败")
                else:
                    st.warning("⚠️ 请在侧边栏设置DeepSeek API密钥以使用AI分析功能")
                    st.info("💡 提示：您可以在DeepSeek官网注册获取API密钥")
                    
                    # 提供DeepSeek网页版备选方案
                    st.divider()
                    st.info("🚀 免费备选方案")
                    st.markdown("您可以直接使用DeepSeek网页版进行免费的股票分析：")
                    st.markdown("[🌐 点击跳转到DeepSeek网页版](https://chat.deepseek.com/)")
                    
                    # 生成PDF报告（即使没有API密钥）
                    st.divider()
                    st.subheader("📄 PDF报告生成")
                    
                    # 准备股票数据
                    stock_data = {
                        'symbol': st.session_state['symbol'],
                        'name': st.session_state['name'],
                        'kline_data': st.session_state['kline_data']
                    }
                    
                    # 生成PDF报告
                    with st.spinner("📊 正在生成PDF报告..."):
                        analysis_result = "AI分析暂时不可用，以下是股票数据摘要"
                        pdf_path = generate_pdf_report(stock_data, analysis_result)
                    
                    if pdf_path:
                        st.success(f"✅ PDF报告生成成功: {pdf_path}")
                        
                        # 添加PDF报告下载功能
                        with open(pdf_path, "rb") as f:
                            pdf_data = f.read()
                        
                        download_filename = f"{stock_data['name']}_{stock_data['symbol']}_分析报告_{datetime.now().strftime('%Y%m%d_%H%M%S')}.pdf"
                        
                        st.download_button(
                            label="📥 下载PDF报告",
                            data=pdf_data,
                            file_name=download_filename,
                            mime="application/pdf"
                        )
                    else:
                        st.error("❌ PDF报告生成失败")
                
                # ====== 传统技术分析 (辅助功能) ======
                st.divider()
                st.subheader("📊 传统技术分析")
                
                # 显示K线图表
                fig = plot_candlestick(
                    st.session_state['kline_data'],
                    st.session_state['symbol'],
                    st.session_state['name']
                )
                st.plotly_chart(fig, use_container_width=True)
                
                # 技术指标解读
                kline = st.session_state['kline_data']
                kline = calculate_indicators(kline)
                
                # 获取技术指标状态
                indicator_status = get_technical_status(kline)
                
                col_a, col_b, col_c, col_d = st.columns(4)
                
                with col_a:
                    st.metric("均线状态", indicator_status.get('ma_status', '未知'))
                
                with col_b:
                    st.metric("MACD", indicator_status.get('macd_status', '未知'))
                
                with col_c:
                    st.metric("KDJ", indicator_status.get('kdj_status', '未知'))
                
                with col_d:
                    st.metric("OBV", indicator_status.get('obv_trend', '未知'))
else:
    st.error("❌ 无法获取A股数据，请检查网络连接")
    
    st.info("""
    💡 解决方案:
    1. 检查网络是否正常访问国内网站
    2. 如果使用代理软件，请确保代理正常工作
    3. 或者暂时关闭代理尝试
    """)

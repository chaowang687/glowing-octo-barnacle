#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
A股量化选股系统 V2 - 增强版
功能：多标签页界面，集成技术面、基本面、板块效应、缠论分析
"""

import os
for var in ['HTTP_PROXY', 'HTTPS_PROXY', 'http_proxy', 'https_proxy']:
    os.environ.pop(var, None)
os.environ['no_proxy'] = '*'

import streamlit as st
import pandas as pd
import numpy as np
from datetime import datetime
import plotly.graph_objects as go
from plotly.subplots import make_subplots

# 导入模块
from tencent_source import TencentDataSource
from sector_analysis import SectorAnalysis
from fundamental import FundamentalSelector
from hot_stocks import HotStockSource
from chanlun_analyzer import ChanlunAnalyzer

# 初始化
st.set_page_config(
    page_title="A股量化选股系统 V2",
    page_icon="📈",
    layout="wide",
    initial_sidebar_state="expanded"
)

# 样式 - 增强对比度
st.markdown("""
<style>
    /* 全局深色主题 */
    .stApp { 
        background-color: #0E1117; 
    }
    body { 
        color: #FFFFFF !important; 
        background-color: #0E1117;
    }
    
    /* 所有文字强制白色 */
    .stMarkdown, p, div, span, label, li, th, td {
        color: #FFFFFF !important;
    }
    
    /* 标题增强对比度 */
    h1, h2, h3, h4, h5, h6 {
        color: #FFFFFF !important;
        font-weight: 600;
    }
    
    /* 顶栏深色 */
    header[data-testid="stHeader"] {
        background-color: #1A1A2E;
    }
    
    /* 侧边栏深色 */
    [data-testid="stSidebar"] {
        background-color: #1A1A2E;
    }
    [data-testid="stSidebar"] .stMarkdown,
    [data-testid="stSidebar"] p,
    [data-testid="stSidebar"] div,
    [data-testid="stSidebar"] label,
    [data-testid="stSidebar"] h1,
    [data-testid="stSidebar"] h2,
    [data-testid="stSidebar"] h3 {
        color: #FFFFFF !important;
    }
    
    /* 标签页样式 */
    .stTabs [data-baseweb="tab-list"] { gap: 10px; }
    .stTabs [data-baseweb="tab"] { 
        background-color: #262730; 
        border-radius: 8px;
        padding: 8px 16px;
    }
    .stTabs [data-baseweb="tab"][aria-selected="true"] {
        background-color: #00D4FF !important;
        color: #000000 !important;
    }
    
    /* 表格样式 - 强制白字 */
    .dataframe {
        color: #FFFFFF !important;
        background-color: transparent !important;
    }
    .dataframe th {
        background-color: #1A1A2E !important;
        color: #FFFFFF !important;
    }
    .dataframe td {
        background-color: transparent !important;
        color: #FFFFFF !important;
    }
    .dataframe tr:nth-child(even) {
        background-color: #1A1A2E !important;
    }
    
    /* Streamlit表格 */
    [data-testid="stDataFrame"] {
        background-color: transparent !important;
    }
    [data-testid="stDataFrame"] div {
        color: #FFFFFF !important;
    }
    
    /* 按钮样式 */
    .stButton > button {
        background-color: #00D4FF;
        color: #000000;
        font-weight: 600;
    }
    .stButton > button:hover {
        background-color: #00B8E6;
    }
    
    /* 输入框 */
    .stTextInput input, .stTextArea textarea {
        background-color: #262730 !important;
        color: #FFFFFF !important;
        border: 1px solid #404040;
    }
    .stTextInput input::placeholder {
        color: #AAAAAA !important;
    }
    
    /* 下拉选择框 */
    .stSelectbox div[data-baseweb="select"] {
        background-color: #262730 !important;
    }
    .stSelectbox span {
        color: #FFFFFF !important;
    }
    
    /* 滑块 */
    .stSlider [data-baseweb="slider"] {
        background-color: #404040;
    }
    
    /* 指标卡片 */
    [data-testid="stMetricValue"] {
        color: #FFFFFF !important;
    }
    [data-testid="stMetricLabel"] {
        color: #CCCCCC !important;
    }
    
    /* 警告和信息框 */
    .stAlert {
        background-color: #1A1A2E !important;
        color: #FFFFFF !important;
    }
    .stAlert div {
        color: #FFFFFF !important;
    }
    
    /* 分隔线 */
    hr {
        border-color: #404040 !important;
    }
    
    /* 滚动条 */
    ::-webkit-scrollbar {
        width: 8px;
        height: 8px;
    }
    ::-webkit-scrollbar-track {
        background: #1A1A2E;
    }
    ::-webkit-scrollbar-thumb {
        background: #404040;
        border-radius: 4px;
    }
    ::-webkit-scrollbar-thumb:hover {
        background: #505050;
    }
    
    /* 成功/警告/错误消息 */
    .stSuccess, .stWarning, .stError, .stInfo {
        background-color: #1A1A2E !important;
    }
    .stSuccess div, .stWarning div, .stError div, .stInfo div {
        color: #FFFFFF !important;
    }
</style>
""", unsafe_allow_html=True)

# 初始化数据源
@st.cache_data(ttl=180)
def get_market_data():
    """获取市场数据"""
    tencent = TencentDataSource()
    return tencent.get_realtime_quotes(5000)  # 获取更多股票

@st.cache_data(ttl=3600)
def get_stock_kline(symbol):
    """获取K线"""
    tencent = TencentDataSource()
    return tencent.get_stock_kline(symbol)

@st.cache_data(ttl=300)
def get_sector_data():
    """获取板块数据"""
    sector = SectorAnalysis()
    return sector.get_sector_strength(20)

@st.cache_data(ttl=60)
def get_hot_stocks_data():
    """获取热点股票数据 - 使用腾讯API"""
    tencent = TencentDataSource()
    # 获取更多股票按涨跌幅排序
    df = tencent.get_realtime_quotes(50)
    if df is not None and len(df) > 0:
        return df.sort_values('涨跌幅', ascending=False).head(30)
    return df

@st.cache_data(ttl=60)
def get_turnover_data():
    """获取换手率排行 - 使用腾讯API"""
    # 腾讯API没有换手率字段，使用涨跌幅排行代替
    tencent = TencentDataSource()
    df = tencent.get_realtime_quotes(50)
    if df is not None and len(df) > 0:
        return df.sort_values('涨跌幅', ascending=False).head(30)
    return df

@st.cache_data(ttl=60)
def get_amount_data():
    """获取成交额排行 - 使用腾讯API"""
    tencent = TencentDataSource()
    df = tencent.get_realtime_quotes(50)
    if df is not None and len(df) > 0:
        return df.sort_values('成交额', ascending=False).head(30)
    return df

# ==================== 主界面 ====================

st.title("📈 A股全能量化选股系统 V2")
st.caption("融合技术面 · 基本面 · 板块效应 · 缠论结构")

# 创建标签页
tab1, tab2, tab3, tab4, tab5, tab6 = st.tabs([
    "📊 市场概览", 
    "🎯 技术选股", 
    "💰 基本面选股",
    "🔥 板块效应",
    "📈 个股分析",
    "⭐ 自选股"
])

# 初始化自选股列表
if 'watchlist' not in st.session_state:
    st.session_state.watchlist = ['002050']  # 默认自选: 三花智控

# ==================== 标签页1: 市场概览 ====================
with tab1:
    st.header("📊 市场概览")
    
    with st.spinner("加载市场数据..."):
        df = get_market_data()
    
    if df is not None and len(df) > 0:
        # 统计
        col1, col2, col3, col4 = st.columns(4)
        with col1:
            st.metric("股票总数", len(df))
        with col2:
            up = len(df[df['涨跌幅'] > 0])
            st.metric("上涨", f"{up} ↑", f"{up/len(df)*100:.1f}%")
        with col3:
            down = len(df[df['涨跌幅'] < 0])
            st.metric("下跌", f"{down} ↓", f"-{down/len(df)*100:.1f}%")
        with col4:
            avg = df['涨跌幅'].mean()
            st.metric("平均涨幅", f"{avg:.2f}%")
        
        # 涨幅榜
        st.subheader("🔥 涨幅榜 TOP 20")
        top_gainers = df.nlargest(20, '涨跌幅')[['代码', '名称', '最新价', '涨跌幅', '成交额']]
        st.dataframe(
            top_gainers.style.format({'最新价': '{:.2f}', '涨跌幅': '{:.2f}%', '成交额': '{:.0f}'}),
            use_container_width=True
        )
        
        # 跌幅榜
        st.subheader("📉 跌幅榜 TOP 20")
        top_losers = df.nsmallest(20, '涨跌幅')[['代码', '名称', '最新价', '涨跌幅', '成交额']]
        st.dataframe(
            top_losers.style.format({'最新价': '{:.2f}', '涨跌幅': '{:.2f}%', '成交额': '{:.0f}'}),
            use_container_width=True
        )
        
        # ====== 热点股票 ======
        st.divider()
        st.subheader("🔥 热点股票排行")
        
        # 热点股票Tab
        hs_col1, hs_col2, hs_col3 = st.columns(3)
        
        with hs_col1:
            st.markdown("**📈 涨跌幅排行**")
            hot_df = get_hot_stocks_data()
            if hot_df is not None and len(hot_df) > 0:
                st.dataframe(
                    hot_df[['代码', '名称', '最新价', '涨跌幅']].head(15)
                    .style.format({'最新价': '{:.2f}', '涨跌幅': '{:.2f}%'}),
                    use_container_width=True,
                    height=400
                )
        
        with hs_col2:
            st.markdown("**🔄 活跃股排行**")
            turnover_df = get_turnover_data()
            if turnover_df is not None and len(turnover_df) > 0:
                st.dataframe(
                    turnover_df[['代码', '名称', '最新价', '涨跌幅', '成交额']].head(15)
                    .style.format({'最新价': '{:.2f}', '涨跌幅': '{:.2f}%', '成交额': '{:.0f}'}),
                    use_container_width=True,
                    height=400
                )
        
        with hs_col3:
            st.markdown("**💰 成交额排行**")
            amount_df = get_amount_data()
            if amount_df is not None and len(amount_df) > 0:
                st.dataframe(
                    amount_df[['代码', '名称', '最新价', '涨跌幅', '成交额']].head(15)
                    .style.format({'最新价': '{:.2f}', '涨跌幅': '{:.2f}%'}),
                    use_container_width=True,
                    height=400
                )

# ==================== 标签页2: 技术选股 ====================
with tab2:
    st.header("🎯 技术选股")
    
    # 筛选条件
    col1, col2, col3 = st.columns(3)
    with col1:
        min_change = st.slider("最小涨幅%", -10, 10, 3)
    with col2:
        min_vol = st.number_input("最小成交额(亿)", 0.0, 100.0, 1.0, 0.5)
    with col3:
        trend_type = st.selectbox("均线形态", ["全部", "多头排列", "空头排列"])
    
    df = get_market_data()
    
    if df is not None and len(df) > 0:
        # 筛选
        filtered = df.copy()
        filtered = filtered[filtered['涨跌幅'] >= min_change]
        
        if '成交额' in filtered.columns:
            filtered['成交额_亿'] = filtered['成交额'] / 1e8
            filtered = filtered[filtered['成交额_亿'] >= min_vol]
        
        st.write(f"筛选出 **{len(filtered)}** 只股票")
        
        # 显示
        st.dataframe(
            filtered[['代码', '名称', '最新价', '涨跌幅', '成交额_亿']].head(50)
            .style.format({'最新价': '{:.2f}', '涨跌幅': '{:.2f}%', '成交额_亿': '{:.1f}亿'}),
            use_container_width=True
        )

# ==================== 标签页3: 基本面选股 ====================
with tab3:
    st.header("💰 基本面选股")
    
    st.info("💡 基本面数据需要基本面数据源支持，当前显示技术面数据")
    
    df = get_market_data()
    
    if df is not None and len(df) > 0:
        # 按涨幅排序
        st.subheader("📊 热门绩优股")
        
        # 模拟基本面排序（实际需要基本面数据）
        filtered = df.copy()
        filtered = filtered.sort_values('涨跌幅', ascending=False)
        
        st.dataframe(
            filtered[['代码', '名称', '最新价', '涨跌幅']].head(30)
            .style.format({'最新价': '{:.2f}', '涨跌幅': '{:.2f}%'}),
            use_container_width=True
        )

# ==================== 标签页4: 板块效应 ====================
with tab4:
    st.header("🔥 板块效应分析")
    
    with st.spinner("加载板块数据..."):
        try:
            sector = SectorAnalysis()
            sectors = sector.get_sector_strength(20)
            
            if sectors is not None and len(sectors) > 0:
                st.subheader("📈 强势板块 TOP 20")
                
                # 柱状图
                fig = go.Figure()
                fig.add_trace(go.Bar(
                    x=sectors['板块名称'].head(15),
                    y=sectors['涨跌幅'].head(15),
                    marker_color=['#FF2E2E' if x > 0 else '#00F000' for x in sectors['涨跌幅'].head(15)]
                ))
                fig.update_layout(
                    title="板块涨跌幅",
                    template="plotly_dark",
                    height=400,
                    paper_bgcolor='#0E1117',
                    plot_bgcolor='#0E1117'
                )
                st.plotly_chart(fig, use_container_width=True)
                
                # 表格
                st.dataframe(
                    sectors[['板块名称', '涨跌幅', 'RPS']].head(20)
                    .style.format({'涨跌幅': '{:.2f}%', 'RPS': '{:.1f}'}),
                    use_container_width=True
                )
                
                # 板块效应领涨股
                st.subheader("🚀 板块效应领涨股")
                effect_stocks = sector.get_sector_effect_stocks(
                    min_strength=70, 
                    min_leader_change=5.0,
                    top_sectors=15
                )
                
                if effect_stocks:
                    effect_df = pd.DataFrame(effect_stocks)
                    st.dataframe(
                        effect_df[['股票代码', '股票名称', '所属板块', '个股涨跌幅', '板块RPS']].head(20)
                        .style.format({'个股涨跌幅': '{:.2f}%', '板块RPS': '{:.1f}'}),
                        use_container_width=True
                    )
                else:
                    st.write("暂无符合条件的板块效应股")
                    
        except Exception as e:
            st.error(f"获取板块数据失败: {e}")

# ==================== 标签页5: 个股分析 ====================
with tab5:
    st.header("📈 个股详细分析")
    
    # 创建持仓股分析区域
    st.subheader("💼 持仓股分析")
    
    # 持仓股输入区域
    col_h1, col_h2, col_h3 = st.columns([2, 1, 1])
    with col_h1:
        # 持仓股列表输入（支持多只，逗号分隔）
        holdings_input = st.text_area(
            "输入持仓股票（代码或名称，多只用逗号分隔）",
            value="600519,000858,600036",
            height=60,
            help="例如: 600519,茅台,600036,招商银行"
        )
    
    # 解析持仓股
    def parse_holdings(input_str):
        """解析持仓股输入"""
        holdings = []
        if not input_str:
            return holdings
        
        # 分割并清理
        items = [item.strip() for item in input_str.replace('，', ',').split(',') if item.strip()]
        
        # 尝试匹配股票
        market_data = get_market_data()
        if market_data is not None and len(market_data) > 0:
            for item in items:
                # 直接匹配代码
                match = market_data[market_data['代码'] == item.zfill(6)]
                if len(match) == 0:
                    # 匹配名称
                    match = market_data[market_data['名称'].str.contains(item, na=False)]
                if len(match) > 0:
                    for _, row in match.iterrows():
                        holdings.append({
                            '代码': row['代码'],
                            '名称': row['名称'],
                            '最新价': row.get('最新价', 0),
                            '涨跌幅': row.get('涨跌幅', 0)
                        })
                        if len(holdings) >= 20:  # 最多20只
                            break
        return holdings
    
    holdings = parse_holdings(holdings_input)
    
    if holdings:
        with col_h2:
            st.write(f"已识别 {len(holdings)} 只股票")
        with col_h3:
            analyze_holdings_btn = st.button("📊 分析持仓")
        
        if analyze_holdings_btn:
            with st.spinner("分析持仓股..."):
                # 批量获取持仓股K线
                hold_results = []
                for h in holdings:
                    code = h['代码']
                    try:
                        kline = get_stock_kline(code)
                        if kline is not None and len(kline) >= 30:
                            # 缠论分析
                            analyzer = ChanlunAnalyzer()
                            chan_result = analyzer.analyze(kline)
                            
                            # 技术指标
                            kline['MA20'] = kline['收盘'].rolling(20).mean()
                            kline['EMA12'] = kline['收盘'].ewm(span=12).mean()
                            kline['EMA26'] = kline['收盘'].ewm(span=26).mean()
                            kline['DIF'] = kline['EMA12'] - kline['EMA26']
                            kline['DEA'] = kline['DIF'].ewm(span=9).mean()
                            
                            # 判断状态
                            ma_status = "多头" if kline['MA20'].iloc[-1] > kline['MA20'].iloc[-5] else "空头"
                            macd_status = "金叉" if kline['DIF'].iloc[-1] > kline['DEA'].iloc[-1] else "死叉"
                            trend = chan_result.get('trend', '整理')
                            
                            # 缠论信号
                            signals = chan_result.get('signals', [])
                            buy_signals = [s for s in signals if '买' in str(s.get('type', ''))]
                            sell_signals = [s for s in signals if '卖' in str(s.get('type', ''))]
                            
                            signal_text = ""
                            if buy_signals:
                                signal_text = f"买:{buy_signals[0].get('type', '')}"
                            elif sell_signals:
                                signal_text = f"卖:{sell_signals[0].get('type', '')}"
                            else:
                                signal_text = "观望"
                            
                            hold_results.append({
                                '代码': code,
                                '名称': h['名称'],
                                '最新价': h['最新价'],
                                '涨跌幅': h['涨跌幅'],
                                '均线': ma_status,
                                'MACD': macd_status,
                                '缠论趋势': trend,
                                '信号': signal_text
                            })
                    except Exception as e:
                        continue
                
                if hold_results:
                    hold_df = pd.DataFrame(hold_results)
                    
                    # 显示持仓分析结果
                    st.subheader("📊 持仓分析结果")
                    
                    # 统计
                    col_s1, col_s2, col_s3, col_s4 = st.columns(4)
                    with col_s1:
                        up_count = len(hold_df[hold_df['涨跌幅'] > 0])
                        st.metric("上涨", f"{up_count}/{len(hold_df)}")
                    with col_s2:
                        duo_tou = len(hold_df[hold_df['均线'] == '多头'])
                        st.metric("多头排列", f"{duo_tou}/{len(hold_df)}")
                    with col_s3:
                        golden = len(hold_df[hold_df['MACD'] == '金叉'])
                        st.metric("MACD金叉", f"{golden}/{len(hold_df)}")
                    with col_s4:
                        buy_signals_count = len([r for r in hold_results if '买' in r['信号']])
                        st.metric("买入信号", f"{buy_signals_count}/{len(hold_df)}")
                    
                    # 持仓表格
                    st.dataframe(
                        hold_df.style.format({
                            '最新价': '{:.2f}',
                            '涨跌幅': '{:.2f}%'
                        }),
                        use_container_width=True,
                        height=400
                    )
                    
                    # 推荐操作
                    st.subheader("💡 持仓建议")
                    buy_stocks = hold_df[hold_df['信号'].str.contains('买')]
                    if len(buy_stocks) > 0:
                        st.success(f"关注买入: {', '.join(buy_stocks['名称'].tolist())}")
                    
                    sell_stocks = hold_df[hold_df['信号'].str.contains('卖')]
                    if len(sell_stocks) > 0:
                        st.warning(f"注意卖出: {', '.join(sell_stocks['名称'].tolist())}")
                else:
                    st.warning("无法获取持仓股数据")
    else:
        st.info("请输入持仓股票代码或名称")
    
    st.divider()
    
    # ====== 个股查询分析 ======
    st.subheader("🔍 个股查询分析")
    
    # 输入股票代码或名称
    col1, col2 = st.columns([2, 1])
    with col1:
        search_input = st.text_input(
            "输入股票代码或名称搜索",
            "600000",
            help="支持代码或名称模糊查询"
        )
    
    # 解析输入
    def resolve_symbol(input_str):
        """解析输入为股票代码"""
        input_str = input_str.strip()
        if not input_str:
            return None, None
        
        market_data = get_market_data()
        if market_data is not None and len(market_data) > 0:
            # 直接匹配代码
            match = market_data[market_data['代码'] == input_str.zfill(6)]
            if len(match) > 0:
                return match.iloc[0]['代码'], match.iloc[0]['名称']
            
            # 模糊匹配名称
            match = market_data[market_data['名称'].str.contains(input_str, na=False)]
            if len(match) > 0:
                # 返回第一个匹配
                return match.iloc[0]['代码'], match.iloc[0]['名称']
        
        return None, None
    
    symbol, found_name = resolve_symbol(search_input)
    
    if symbol:
        st.write(f"已找到: **{found_name}** ({symbol})")
        analyze_btn = st.button("🔍 分析", key="analyze_single")
        
        if analyze_btn:
            with st.spinner(f"分析 {symbol}..."):
                # 获取K线
                kline = get_stock_kline(symbol)
                
                if kline is not None and len(kline) > 0:
                    # 计算技术指标
                    kline['MA5'] = kline['收盘'].rolling(5).mean()
                    kline['MA10'] = kline['收盘'].rolling(10).mean()
                    kline['MA20'] = kline['收盘'].rolling(20).mean()
                    
                    # MACD
                    kline['EMA12'] = kline['收盘'].ewm(span=12).mean()
                    kline['EMA26'] = kline['收盘'].ewm(span=26).mean()
                    kline['DIF'] = kline['EMA12'] - kline['EMA26']
                    kline['DEA'] = kline['DIF'].ewm(span=9).mean()
                    kline['MACD'] = (kline['DIF'] - kline['DEA']) * 2
                    
                    # KDJ
                    low_9 = kline['最低'].rolling(9).min()
                    high_9 = kline['最高'].rolling(9).max()
                    kline['RSV'] = (kline['收盘'] - low_9) / (high_9 - low_9) * 100
                    kline['K'] = kline['RSV'].ewm(3).mean()
                    kline['D'] = kline['K'].ewm(3).mean()
                    kline['J'] = 3 * kline['K'] - 2 * kline['D']
                    
                    # 绘制K线图
                    fig = make_subplots(
                        rows=4, cols=1,
                        shared_xaxes=True,
                        vertical_spacing=0.05,
                        row_heights=[0.5, 0.15, 0.15, 0.15],
                        subplot_titles=('K线 & 均线', '成交量', 'MACD', 'KDJ')
                    )
                    
                    # K线
                    fig.add_trace(go.Candlestick(
                        x=kline.index,
                        open=kline['开盘'],
                        high=kline['最高'],
                        low=kline['最低'],
                        close=kline['收盘'],
                        name='K线'
                    ), row=1, col=1)
                    
                    # 均线
                    for ma in ['MA5', 'MA10', 'MA20']:
                        fig.add_trace(go.Scatter(
                            x=kline.index, y=kline[ma],
                            mode='lines', name=ma,
                            line=dict(width=1)
                        ), row=1, col=1)
                    
                    # 成交量
                    colors = ['#FF2E2E' if kline['收盘'].iloc[i] >= kline['开盘'].iloc[i] else '#00F000' 
                              for i in range(len(kline))]
                    fig.add_trace(go.Bar(
                        x=kline.index, y=kline['成交量'],
                        marker_color=colors,
                        name='成交量'
                    ), row=2, col=1)
                    
                    # MACD
                    fig.add_trace(go.Bar(
                        x=kline.index, y=kline['MACD'],
                        marker_color='#6366f1',
                        name='MACD'
                    ), row=3, col=1)
                    fig.add_trace(go.Scatter(
                        x=kline.index, y=kline['DIF'],
                        mode='lines', name='DIF',
                        line=dict(width=1)
                    ), row=3, col=1)
                    fig.add_trace(go.Scatter(
                        x=kline.index, y=kline['DEA'],
                        mode='lines', name='DEA',
                        line=dict(width=1)
                    ), row=3, col=1)
                    
                    # KDJ
                    fig.add_trace(go.Scatter(
                        x=kline.index, y=kline['K'],
                        mode='lines', name='K',
                        line=dict(width=1)
                    ), row=4, col=1)
                    fig.add_trace(go.Scatter(
                        x=kline.index, y=kline['D'],
                        mode='lines', name='D',
                        line=dict(width=1)
                    ), row=4, col=1)
                    fig.add_trace(go.Scatter(
                        x=kline.index, y=kline['J'],
                        mode='lines', name='J',
                        line=dict(width=1)
                    ), row=4, col=1)
                    
                    fig.update_layout(
                        title=f'{symbol} 日K线',
                        template='plotly_dark',
                        height=700,
                        showlegend=True,
                        xaxis_rangeslider_visible=False,
                        paper_bgcolor='#0E1117',
                        plot_bgcolor='#0E1117'
                    )
                    
                    st.plotly_chart(fig, use_container_width=True)
                    
                    # 技术指标解读
                    st.subheader("📊 技术指标状态")
                    
                    col1, col2, col3, col4 = st.columns(4)
                    with col1:
                        ma_status = "多头↑" if kline['MA20'].iloc[-1] > kline['MA20'].iloc[-5] else "空头↓"
                        st.metric("均线状态", ma_status)
                    with col2:
                        macd_status = "金叉↑" if kline['MACD'].iloc[-1] > 0 else "死叉↓"
                        st.metric("MACD", macd_status)
                    with col3:
                        k_val = kline['J'].iloc[-1]
                        kdj_status = "超买" if k_val > 100 else "超卖" if k_val < 0 else "正常"
                        st.metric("KDJ", kdj_status)
                    with col4:
                        price = kline['收盘'].iloc[-1]
                        change = kline['涨跌幅'].iloc[-1] if '涨跌幅' in kline.columns else 0
                        st.metric("最新价", f"{price:.2f}", f"{change:.2f}%")
                    
                    # ====== 缠论分析 ======
                    st.divider()
                    st.subheader("🌀 缠论结构分析")
                    
                    with st.spinner("分析缠论结构..."):
                        analyzer = ChanlunAnalyzer()
                        chan_result = analyzer.analyze(kline)
                    
                    if chan_result['status'] == '成功':
                        # 缠论简报
                        st.info(f"📋 **{chan_result['summary']}**")
                        
                        # 缠论状态
                        cl_col1, cl_col2, cl_col3, cl_col4 = st.columns(4)
                        with cl_col1:
                            st.metric("当前趋势", chan_result['trend'])
                        with cl_col2:
                            bi_count = len(chan_result.get('bi', []))
                            st.metric("笔数量", f"{bi_count}")
                        with cl_col3:
                            zhongshu_count = len(chan_result.get('zhongshu', []))
                            st.metric("中枢数量", f"{zhongshu_count}")
                        with cl_col4:
                            signals = chan_result.get('signals', [])
                            if len(signals) > 0:
                                last_signal = signals[-1].get('type', '无')
                            else:
                                last_signal = '无'
                            st.metric("最新信号", last_signal)
                        
                        # 买卖点信号
                        if signals and len(signals) > 0:
                            st.subheader("🎯 缠论买卖点")
                            
                            # 买入信号
                            buy_signals = [s for s in signals if '买' in str(s.get('type', ''))]
                            if buy_signals:
                                st.success(f"🟢 买入信号: {', '.join([s.get('type', '') for s in buy_signals])}")
                            
                            # 卖出信号
                            sell_signals = [s for s in signals if '卖' in str(s.get('type', ''))]
                            if sell_signals:
                                st.error(f"🔴 卖出信号: {', '.join([s.get('type', '') for s in sell_signals])}")
                            
                            # 显示所有信号
                            if len(signals) <= 10:
                                signal_df = pd.DataFrame(signals)
                                if not signal_df.empty:
                                    st.dataframe(signal_df, use_container_width=True)
                        else:
                            st.write("暂无缠论买卖点信号")
                    else:
                        st.warning(f"缠论分析: {chan_result.get('summary', '分析失败')}")
                    
                else:
                    st.error("无法获取K线数据")

# ==================== 标签页6: 自选股 ====================
with tab6:
    st.header("⭐ 自选股管理")
    
    # 自选股操作区域
    st.subheader("➕ 添加自选股")
    
    # 输入股票代码或名称
    add_col1, add_col2 = st.columns([3, 1])
    with add_col1:
        watch_input = st.text_input(
            "输入股票代码或名称（支持多只，逗号分隔）",
            placeholder="例如: 600519,000858,茅台",
            key="watch_input"
        )
    with add_col2:
        if st.button("添加", type="primary"):
            if watch_input:
                # 解析输入
                items = [item.strip() for item in watch_input.replace('，', ',').split(',') if item.strip()]
                market_data = get_market_data()
                added_count = 0
                
                for item in items:
                    # 尝试匹配股票
                    if market_data is not None:
                        match = market_data[market_data['代码'] == item.zfill(6)]
                        if len(match) == 0:
                            match = market_data[market_data['名称'].str.contains(item, na=False)]
                        
                        if len(match) > 0:
                            code = match.iloc[0]['代码']
                            if code not in st.session_state.watchlist:
                                st.session_state.watchlist.append(code)
                                added_count += 1
                
                if added_count > 0:
                    st.success(f"成功添加 {added_count} 只股票到自选")
                else:
                    st.warning("未找到匹配的股票")
    
    # 显示/删除自选股
    st.divider()
    st.subheader("📋 我的自选股")
    
    if len(st.session_state.watchlist) > 0:
        # 获取自选股实时数据
        watch_df = get_market_data()
        if watch_df is not None:
            watch_stocks = watch_df[watch_df['代码'].isin(st.session_state.watchlist)]
            
            # 显示自选股数据
            if len(watch_stocks) > 0:
                # 添加删除按钮
                st.dataframe(
                    watch_stocks[['代码', '名称', '最新价', '涨跌幅', '成交额']],
                    use_container_width=True
                )
                
                # 删除自选股
                st.subheader("🗑️ 删除自选股")
                delete_col1, delete_col2 = st.columns([3, 1])
                with delete_col1:
                    delete_code = st.selectbox(
                        "选择要删除的股票",
                        options=st.session_state.watchlist,
                        key="delete_select"
                    )
                with delete_col2:
                    if st.button("删除"):
                        if delete_code in st.session_state.watchlist:
                            st.session_state.watchlist.remove(delete_code)
                            st.success(f"已删除 {delete_code}")
                            st.rerun()
            else:
                st.warning("自选股数据获取失败")
        
        # 清空全部
        if st.button("清空全部自选", type="secondary"):
            st.session_state.watchlist = []
            st.rerun()
    else:
        st.info("暂无自选股，请添加")

# 侧边栏
st.sidebar.title("⚙️ 系统设置")
st.sidebar.info("数据来源: 腾讯财经")

if st.sidebar.button("🔄 刷新数据"):
    st.cache_data.clear()
    st.rerun()

st.sidebar.markdown("---")
st.sidebar.caption("© 2026 A-Quant V2")

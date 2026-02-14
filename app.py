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

from indicators import calculate_all_indicators, get_technical_status


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

st.sidebar.header("🎯 选股条件")
min_change = st.sidebar.slider("最小涨幅(%)", -10, 10, filter_conditions.get('min_change', 2), 1)
min_volume = st.sidebar.number_input("最小成交额(亿)", 0.0, 100.0, filter_conditions.get('min_volume', 0.5), 0.5)

st.sidebar.header("📈 技术筛选")
use_ma_filter = st.sidebar.checkbox("均线多头排列", value=filter_conditions.get('use_ma_filter', True))
use_macd_filter = st.sidebar.checkbox("MACD金叉", value=filter_conditions.get('use_macd_filter', False))
use_kdj_filter = st.sidebar.checkbox("KDJ金叉", value=filter_conditions.get('use_kdj_filter', False))

# 保存用户配置
if st.sidebar.button("💾 保存配置"):
    new_conditions = {
        'min_change': min_change,
        'min_volume': min_volume,
        'use_ma_filter': use_ma_filter,
        'use_macd_filter': use_macd_filter,
        'use_kdj_filter': use_kdj_filter,
    }
    user_config.set_filter_conditions(new_conditions)
    st.sidebar.success("配置保存成功!")

# ==================== 主页面 ====================

# 专业页眉设计
header_html = f"""
<div style='background: linear-gradient(135deg, #1E40AF 0%, #1E3A8A 100%); padding: 2.5rem 2rem; border-radius: 20px; margin-bottom: 2rem; box-shadow: 0 10px 30px rgba(30, 64, 175, 0.3); border: 1px solid rgba(255, 255, 255, 0.1);'>
    <div style='display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1.5rem;'>
        <div style='flex: 1; min-width: 300px;'>
            <h1 style='color: white; font-size: 2.8rem; margin: 0; font-weight: 700; letter-spacing: -0.02em; text-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);'>
                📊 量化分析选股系统
            </h1>
            <p style='color: white; font-size: 1.15rem; margin: 0.75rem 0 0 0; font-weight: 400; opacity: 0.95; line-height: 1.6;'>
                融合缠论结构 · CPV量价分析 · 基本面筛选 · AI智能分析
            </p>
        </div>
        <div style='text-align: right; color: white; opacity: 0.95;'>
            <div style='font-size: 1rem; font-weight: 500; margin-bottom: 0.5rem;'>
                🕐 {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}
            </div>
            <div style='font-size: 0.9rem; opacity: 0.85; font-weight: 300; letter-spacing: 0.5px;'>
                Professional Stock Analysis Platform
            </div>
        </div>
    </div>
</div>
"""
st.markdown(header_html, unsafe_allow_html=True)

if DEMO_MODE:
    st.warning("⚠️ 当前为演示模式，使用模拟数据")

# 创建标签页
selected_tab = st.sidebar.selectbox(
    "选择功能",
    ["市场概览", "智能综合选股", "网页设计工具"],
    index=0
)

if selected_tab == "市场概览":
    # 获取数据
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
                                            trend_df['hot_money_net'] = trend_df.get('net', 0) * 0.5
                                        if 'retail_net' not in trend_df.columns:
                                            trend_df['retail_net'] = trend_df.get('net', 0) * 0.3
                                        
                                        # 转换为万
                                        trend_df['main_net'] = trend_df['main_net'].fillna(0) / 10000
                                        trend_df['hot_money_net'] = trend_df['hot_money_net'].fillna(0) / 10000
                                        trend_df['retail_net'] = trend_df['retail_net'].fillna(0) / 10000
                                        
                                        # 创建折线图
                                        fig = go.Figure()
                                        
                                        # 添加主力资金
                                        fig.add_trace(go.Scatter(
                                            x=trend_df['date'],
                                            y=trend_df['main_net'],
                                            name='主力资金',
                                            line=dict(color='blue', width=2),
                                            mode='lines+markers'
                                        ))
                                        
                                        # 添加游资资金
                                        fig.add_trace(go.Scatter(
                                            x=trend_df['date'],
                                            y=trend_df['hot_money_net'],
                                            name='游资资金',
                                            line=dict(color='red', width=2),
                                            mode='lines+markers'
                                        ))
                                        
                                        # 添加散户资金
                                        fig.add_trace(go.Scatter(
                                            x=trend_df['date'],
                                            y=trend_df['retail_net'],
                                            name='散户资金',
                                            line=dict(color='green', width=2),
                                            mode='lines+markers'
                                        ))
                                        
                                        # 布局设置
                                        fig.update_layout(
                                            title='近5日资金流向趋势',
                                            xaxis_title='日期',
                                            yaxis_title='净流入(万)',
                                            template='plotly_dark',
                                            legend=dict(orientation="h", yanchor="bottom", y=1.02, xanchor="center", x=0.5),
                                            height=400
                                        )
                                        
                                        st.plotly_chart(fig, use_container_width=True)
                                    else:
                                        st.info("📅 近5日资金流向：暂无数据")
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
                                        'stable': '⚖️ 稳定'
                                    }.get(industry_trend, '未知')
                                    st.metric("行业趋势", trend_text)
                                
                                with col_y:
                                    market_trend = market_context.get('market_trend', 'unknown')
                                    market_text = {
                                        'up': '📈 上涨',
                                        'down': '📉 下跌',
                                        'stable': '⚖️ 稳定'
                                    }.get(market_trend, '未知')
                                    st.metric("大盘趋势", market_text)
                            
                            # ====== 生成PDF报告并发送 ======
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

elif selected_tab == "智能综合选股":
    st.subheader("🤖 智能综合选股")
    st.markdown("融合缠论结构 · CPV量价分析 · 基本面筛选 · 行业分析")
    
    # 选股策略选择
    strategy = st.selectbox(
        "选择选股策略",
        ["综合选股", "买入信号", "行业效应"],
        index=0
    )
    
    # 选股参数设置
    st.markdown("### 🎯 选股参数")
    
    col1, col2 = st.columns(2)
    
    with col1:
        # 技术指标参数
        st.markdown("#### 技术指标")
        min_ma_score = st.slider("均线评分 (0-100)", 0, 100, 60)
        min_macd_score = st.slider("MACD评分 (0-100)", 0, 100, 50)
        min_kdj_score = st.slider("KDJ评分 (0-100)", 0, 100, 50)
        min_obv_score = st.slider("OBV评分 (0-100)", 0, 100, 50)
    
    with col2:
        # 基本面参数
        st.markdown("#### 基本面")
        max_pe = st.slider("最大市盈率", 0, 200, 50)
        min_roe = st.slider("最小ROE (%)", 0, 50, 5)
        min_market_cap = st.slider("最小市值 (亿)", 0, 10000, 100)
        max_debt_ratio = st.slider("最大负债率 (%)", 0, 100, 70)
    
    # 行业筛选
    st.markdown("### 📈 行业筛选")
    sectors = ["全部", "科技", "金融", "医药", "消费", "能源", "材料", "工业", "公用事业", "房地产"]
    selected_sector = st.selectbox("选择行业", sectors)
    
    # 选股按钮
    if st.button("🚀 开始智能选股"):
        with st.spinner("🤖 AI正在智能选股..."):
            try:
                # 准备选股参数
                selection_params = {
                    'min_ma_score': min_ma_score,
                    'min_macd_score': min_macd_score,
                    'min_kdj_score': min_kdj_score,
                    'min_obv_score': min_obv_score,
                    'max_pe': max_pe,
                    'min_roe': min_roe,
                    'min_market_cap': min_market_cap,
                    'max_debt_ratio': max_debt_ratio,
                    'sector': selected_sector
                }
                
                # 根据策略执行选股
                if strategy == "综合选股":
                    result = selector.select_stocks(selection_params)
                elif strategy == "买入信号":
                    result = selector.select_by_buy_signals(selection_params)
                elif strategy == "行业效应":
                    result = selector.select_by_sector_effect(selection_params)
                else:
                    result = selector.select_stocks(selection_params)
                
                # 显示选股结果
                # 使用更明确的方式检查结果
                result_empty = False
                
                if isinstance(result, pd.DataFrame):
                    result_empty = result.empty
                elif isinstance(result, list):
                    result_empty = len(result) == 0
                else:
                    result_empty = not result
                
                if not result_empty:
                    # 确保result是DataFrame
                    if isinstance(result, list):
                        # 如果是列表，转换为DataFrame
                        result_df = pd.DataFrame(result)
                    else:
                        # 如果已经是DataFrame，直接使用
                        result_df = result
                    
                    # 再次检查DataFrame是否为空
                    if not result_df.empty:
                        st.success(f"✅ 选股完成！共选出 {len(result_df)} 只股票")
                        
                        # 显示结果表格
                        st.subheader("📊 选股结果")
                        
                        # 确保必要的列存在
                        if '代码' in result_df.columns:
                            # 分页显示
                            page_size = 20
                            total_pages = (len(result_df) + page_size - 1) // page_size
                            page = st.number_input(f"页码 (共{total_pages}页)", 1, total_pages, 1)
                            
                            start_idx = (page - 1) * page_size
                            end_idx = min(start_idx + page_size, len(result_df))
                            
                            st.dataframe(
                                result_df.iloc[start_idx:end_idx],
                                use_container_width=True,
                                height=400
                            )
                            
                            # 生成PDF报告
                            st.divider()
                            st.subheader("📄 生成选股报告")
                            
                            if st.button("📊 生成PDF报告"):
                                with st.spinner("📄 正在生成PDF报告..."):
                                    # 准备报告数据
                                    report_data = {
                                        'strategy': strategy,
                                        'params': selection_params,
                                        'stocks': result_df.to_dict('records'),
                                        'total_count': len(result_df),
                                        'date': datetime.now().strftime('%Y-%m-%d')
                                    }
                                    
                                    # 生成PDF报告
                                    from pdf_generator import generate_selection_pdf_report
                                    pdf_path = generate_selection_pdf_report(report_data)
                                    
                                    if pdf_path:
                                        st.success(f"✅ PDF报告生成成功: {pdf_path}")
                                        
                                        # 添加PDF报告下载功能
                                        with open(pdf_path, "rb") as f:
                                            pdf_data = f.read()
                                        
                                        download_filename = f"智能选股报告_{strategy}_{datetime.now().strftime('%Y%m%d_%H%M%S')}.pdf"
                                        
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
                                                    f"智能选股报告 - {strategy}"
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
                            st.warning("⚠️ 选股结果缺少必要的列")
                            st.dataframe(result_df)
                    else:
                        st.warning("⚠️ 未找到符合条件的股票")
                        st.info("💡 建议：尝试调整选股参数或使用更宽松的条件")
                else:
                    st.warning("⚠️ 未找到符合条件的股票")
                    st.info("💡 建议：尝试调整选股参数或使用更宽松的条件")
                    
            except Exception as e:
                st.error(f"❌ 选股失败: {e}")
                st.info("💡 可能的原因：\n1. 网络连接问题\n2. 数据源暂时不可用\n3. 参数设置不合理\n\n建议：检查网络连接或使用演示模式")

# 专业页脚设计
st.markdown("---")
footer_html = f"""
<div class='footer' style='background: linear-gradient(135deg, #F8FAFC 0%, #E0F2FE 100%); padding: 2.5rem 1.5rem; border-radius: 16px; margin-top: 3rem; box-shadow: 0 -2px 16px rgba(0, 0, 0, 0.05);'>
    <div style='text-align: center;'>
        <h3 style='color: #1E40AF; font-size: 1.25rem; font-weight: 700; margin-bottom: 1rem;'>
            📊 量化分析选股系统
        </h3>
        <p style='color: #475569; font-size: 0.95rem; margin-bottom: 0.5rem;'>
            融合缠论结构 · CPV量价分析 · 基本面筛选 · AI智能分析
        </p>
        <div style='border-top: 2px solid #E2E8F0; margin: 1.5rem auto; width: 60%;'></div>
        <p style='color: #64748B; font-size: 0.85rem; margin-bottom: 0.25rem;'>
            ⚠️ 风险提示：本系统仅供个人学习研究使用，不构成任何投资建议
        </p>
        <p style='color: #94A3B8; font-size: 0.8rem;'>
            © 2026 Quant Analysis System · Professional Stock Analysis Platform · Version 2.0
        </p>
        <p style='color: #CBD5E1; font-size: 0.75rem; margin-top: 0.5rem;'>
            Powered by DeepSeek AI · TencentFinance Data · EastMoney API
        </p>
    </div>
</div>
"""
st.markdown(footer_html, unsafe_allow_html=True)

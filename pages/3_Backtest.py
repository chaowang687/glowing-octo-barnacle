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

st.caption("Backtest page build: 2026-02-14 | WF-AB-v1")

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
st.markdown("验证评分公式对个股中短线走势的预测能力")

# 回测参数设置
st.markdown("### 🎯 回测参数")

col1, col2 = st.columns(2)

with col1:
    # 股票选择 - 只需要输入股票代码
    stock_code = st.text_input("输入股票代码", "600519")
    
    # 自动获取股票名称
    stock_name = ""
    if stock_code:
        # 尝试从筛选结果中查找股票名称
        try:
            from data_source import EastMoneyData
            em = EastMoneyData()
            stock_info = em.get_realtime_quote(stock_code)
            if stock_info and '名称' in stock_info:
                stock_name = stock_info['名称']
                st.info(f"股票名称: {stock_name}")
            else:
                st.info("正在获取股票名称...")
        except Exception as e:
            st.info("正在获取股票名称...")
    
    # 回测周期
    backtest_period = st.selectbox(
        "回测周期",
        ["近3个月", "近6个月", "近1年", "近2年"],
        index=1
    )

with col2:
    # 预测周期
    predict_period = st.selectbox(
        "预测周期（中短线）",
        ["3天", "5天", "7天", "10天", "15天"],
        index=2
    )
    
    # 底仓金额设置
    initial_capital = st.number_input(
        "底仓金额（元）",
        min_value=10000,
        max_value=1000000,
        value=300000,
        step=10000,
        help="投资者的初始本金，默认为30万元"
    )
# 显示公式储存状态
if 'ai_optimized_formula' in st.session_state:
    st.success("✅ 当前有储存的AI优化公式")
    st.success("🤖 AI已自主决定最优的买入和卖出阈值")
    st.info("开始回测按钮将直接使用此AI优化公式和AI决定的阈值进行分析")
    # 显示公式的基本信息
    formula_preview = st.session_state['ai_optimized_formula'][:500] + "..." if len(st.session_state['ai_optimized_formula']) > 500 else st.session_state['ai_optimized_formula']
    st.expander("查看储存的AI优化公式").markdown(formula_preview)
else:
    st.info("📝 当前无储存的AI优化公式")
    st.info("点击'🧠 AI优化公式'按钮生成和储存AI优化公式（AI会自主决定最优的买入和卖出阈值）")


# AI优化公式按钮
if st.button("🧠 AI优化公式"):
    with st.spinner("🧠 AI正在设计和优化评分公式..."):
        try:
            # 获取DeepSeek API密钥
            deepseek_api_key = user_config.get_deepseek_api_key()
            
            if not deepseek_api_key:
                st.error("❌ 请先在系统设置中配置DeepSeek API密钥")
            else:
                # 调用DeepSeek API进行公式优化
                analyzer = get_deepseek_analyzer(deepseek_api_key)
                
                # 构建公式优化请求
                formula_request = {
                    "stock_code": stock_code,
                    "stock_name": stock_name,
                    "backtest_period": backtest_period,
                    "predict_period": predict_period
                }
                
                # 构建提示词
                prompt = f"""你是一位专业的量化交易专家，精通技术分析和评分公式设计。请根据以下信息为股票设计一个最优的评分公式，并自主决定最优的买入和卖出阈值：
：
代码：{stock_code}
名称：{stock_name}
周期：{backtest_period}
周期：{predict_period}
息：
：30万元
风格：中短线交易
偏好：稳健
：
计一个满分100分的评分公式，用于预测股票的中短线走势
式应包含以下维度：趋势强度、动量确认、量价配合、风险控制、市场环境适配
每个维度分配合理的权重，并详细说明每个指标的计算方法和评分标准
含扣分项，用于识别风险信号
供具体的评分标准和计算公式
析不同市场环境下公式的有效性
测使用该公式的胜率和预期收益
*自主决定最优的买入和卖出阈值**：基于公式的评分分布和历史数据，分析并确定最优的买入阈值和卖出阈值，以最大化回测胜率和收益率
*考虑资金管理**：基于30万元本金，考虑仓位管理和资金使用效率
**询问关键信息**：请列出你在设计过程中认为需要的其他关键信息，以便更准确地设计公式和阈值
下格式输出结果：
评分公式设计
 趋势强度（30分）：
1：描述和评分标准
2：描述和评分标准
3：描述和评分标准
 动量确认（25分）：
1：描述和评分标准
2：描述和评分标准
3：描述和评分标准
 量价配合（20分）：
1：描述和评分标准
2：描述和评分标准
 风险控制（15分）：
1：描述和评分标准
2：描述和评分标准
3：描述和评分标准
 市场环境适配（10分）：
1：描述和评分标准
 扣分项（直接从总分扣除）：
1：描述和扣分标准
2：描述和扣分标准
3：描述和扣分标准
最优阈值分析
 买入阈值：
值：[具体数值]
依据：[详细说明为什么选择这个阈值]
 卖出阈值：
值：[具体数值]
依据：[详细说明为什么选择这个阈值]
资金管理策略
 仓位管理：
交易资金比例：[百分比]
持仓数量：[数量]
分配策略：[详细说明]
公式有效性分析
 预期胜率：
胜率：[百分比]
 适用市场环境：
：效果如何
：效果如何
市：效果如何
 风险控制措施：
点1：描述和应对措施
点2：描述和应对措施
关键信息需求
 需要的其他关键信息：
1：[详细说明为什么需要这个信息]
2：[详细说明为什么需要这个信息]
3：[详细说明为什么需要这个信息]
释你的设计思路和依据，确保公式具有可操作性和有效性，并且阈值选择合理。同时，考虑30万元本金的资金管理策略，确保资金使用效率和风险控制。"""
                
                # 调用DeepSeek API
                result = analyzer._call_deepseek_api(prompt)
                    
                # 存储AI优化的公式到session_state
                st.session_state['ai_optimized_formula'] = result
                
                # 显示AI设计的公式
                st.success("✅ AI公式设计完成！")
                st.markdown("### 📊 AI设计的评分公式")
                st.markdown(result)
                
                # 提示用户公式和阈值已更新
                st.success("🤖 AI已自主决定最优的买入和卖出阈值")
                st.info("✅ 评分公式和阈值已自动更新，可直接进行回测分析")
                # 显示调试信息
                st.info(f"📋 调试信息：AI优化公式已存储到session_state，长度为 {len(result)} 字符")
        except Exception as e:
            st.error(f"❌ AI公式优化失败: {e}")
            st.info("请检查DeepSeek API密钥是否正确配置，或网络连接是否正常")
            import traceback
            st.code(traceback.format_exc())

# 检查股票数据是否可用
can_backtest = False
if stock_code:
    try:
        # 根据回测周期确定开始日期
        now = datetime.now()
        if backtest_period == "近3个月":
            start_date = (now - timedelta(days=90)).strftime('%Y%m%d')
        elif backtest_period == "近6个月":
            start_date = (now - timedelta(days=180)).strftime('%Y%m%d')
        elif backtest_period == "近1年":
            start_date = (now - timedelta(days=365)).strftime('%Y%m%d')
        elif backtest_period == "近2年":
            start_date = (now - timedelta(days=730)).strftime('%Y%m%d')
        
        end_date = now.strftime('%Y%m%d')
        
        # 获取K线数据
        from data_source import get_kline
        kline_data = get_kline(stock_code, start_date, end_date)
        
        if not kline_data.empty:
            can_backtest = True
            st.success(f"✅ 股票数据可用，共 {len(kline_data)} 条历史数据")
        else:
            st.error("❌ 无法获取股票数据，请检查股票代码是否正确")
    except Exception as e:
        st.error("❌ 无法获取股票数据，请检查股票代码是否正确")
else:
    st.info("请输入股票代码")

# 开始回测按钮 - 只有在有数据的情况下才能点击
if st.button("🚀 开始回测", disabled=not can_backtest):
    with st.spinner("📊 正在进行回测分析..."):
        try:
            # 1. 数据获取和预处理
            st.info(f"正在获取 {stock_code}({stock_name}) 的历史数据...")
            
            # 根据回测周期确定开始日期
            now = datetime.now()
            if backtest_period == "近3个月":
                start_date = (now - timedelta(days=90)).strftime('%Y%m%d')
            elif backtest_period == "近6个月":
                start_date = (now - timedelta(days=180)).strftime('%Y%m%d')
            elif backtest_period == "近1年":
                start_date = (now - timedelta(days=365)).strftime('%Y%m%d')
            elif backtest_period == "近2年":
                start_date = (now - timedelta(days=730)).strftime('%Y%m%d')
            
            end_date = now.strftime('%Y%m%d')
            
            # 获取K线数据
            from data_source import get_kline
            kline_data = get_kline(stock_code, start_date, end_date)
            
            if kline_data.empty:
                st.error("❌ 无法获取股票数据，请检查股票代码是否正确")
            else:
                st.success(f"✅ 成功获取 {len(kline_data)} 条历史数据")
                
                # 2. 评分计算和回测逻辑
                st.info("正在计算评分并进行回测...")
                
                # 计算预测周期天数
                predict_horizon_days = int(predict_period.replace("天", ""))
                strategy_horizon_days = predict_horizon_days
                
                # 回测结果存储
                backtest_results = []
                cumulative_return = 0
                current_capital = initial_capital  # 当前资金
                max_capital = initial_capital  # 最大资金
                min_capital = initial_capital  # 最小资金
                trades = []  # 交易记录
                capital_curve = []  # (date, capital)
                capital_curve.append({'日期': kline_data.index[0], '资金': float(initial_capital)})
                entry_stats = {
                    'score_above_buy': 0,
                    'price_filter_blocked': 0,
                    'executed_buys': 0,
                }
                
                # 中短线交易逻辑 - 加入持仓状态管理
                holding = False  # 持仓状态
                entry_price = 0  # 买入价格
                entry_date = None  # 买入日期
                holding_days = 0  # 持有天数
                max_holding_days = predict_horizon_days
                
                # 止盈止损设置
                take_profit = 8  # 止盈比例（%）
                stop_loss = -5  # 止损比例（%）
                
                # 导入动态评分器和解析器
                from score_formula_parser import ScoreFormulaParser
                from dynamic_scorer import DynamicScorer
                
                # 创建解析器实例
                parser = ScoreFormulaParser()
                
                # 解析DeepSeek返回的结果，获取评分公式
                # 检查是否有AI优化的公式
                if 'ai_optimized_formula' in st.session_state:
                    st.success("📊 使用AI优化的评分公式进行回测")
                    st.info(f"📈 结合个股 {stock_name}({stock_code}) 的历史数据进行分析")
                    # 直接使用存储的AI优化公式
                    formula_text = st.session_state['ai_optimized_formula']
                    formula_info = parser.parse_deepseek_result(formula_text)
                    # 显示公式信息
                    with st.expander("查看AI优化公式解析结果"):
                        st.markdown("### 🧠 AI优化公式内容")
                        st.markdown(formula_text)
                        st.markdown("### 📋 公式解析结果")
                        st.json(formula_info)
                else:
                    st.error("❌ 请先点击'🧠 AI优化公式'按钮生成AI优化公式")
                    st.stop()
                
                # 创建动态评分器实例
                scorer = DynamicScorer(formula_info)
                
                # 显示评分阈值信息
                # 优先使用AI确定的阈值
                ai_buy_threshold = formula_info.get('thresholds', {}).get('buy')
                ai_sell_threshold = formula_info.get('thresholds', {}).get('sell')
                
                buy_threshold = 70
                sell_threshold = 60
                
                if ai_buy_threshold is not None and ai_sell_threshold is not None:
                    buy_threshold = float(ai_buy_threshold)
                    sell_threshold = float(ai_sell_threshold)
                    st.success(f"🤖 使用AI自主决定的评分阈值：买入分={buy_threshold}，卖出分={sell_threshold}")
                else:
                    st.info(f"🎯 使用的评分阈值：买入分={buy_threshold}，卖出分={sell_threshold}")

                if 'backtest_override_buy_threshold' in st.session_state:
                    buy_threshold = float(st.session_state['backtest_override_buy_threshold'])
                    st.info(f"✅ 已应用回测阈值寻优结果：买入分={buy_threshold}")

                # DeepSeek基于个股K线自动判断交易周期与参数（带缓存，避免重复调用）
                ai_param_enabled = st.checkbox("🤖 由DeepSeek判断长/中/短线并自动调整策略参数", value=True)
                if ai_param_enabled and user_config.get_deepseek_api_key():
                    import json
                    import hashlib

                    if 'ai_param_cache' not in st.session_state:
                        st.session_state['ai_param_cache'] = {}

                    last_dt = kline_data.index[-1]
                    key_src = f"{stock_code}|{backtest_period}|{predict_period}|{str(last_dt)}"
                    param_key = hashlib.md5(key_src.encode("utf-8")).hexdigest()
                    ai_params = st.session_state['ai_param_cache'].get(param_key)

                    if ai_params is None:
                        with st.spinner("🤖 DeepSeek 正在判断长/中/短线与策略参数..."):
                            # 仅传递摘要，避免prompt过长
                            recent = kline_data.tail(60).copy()
                            if '日期' in recent.columns:
                                recent_dates = pd.to_datetime(recent['日期'])
                            else:
                                recent_dates = pd.to_datetime(recent.index)
                            recent_close = recent['收盘'].astype(float).tolist()
                            recent_open = recent['开盘'].astype(float).tolist()
                            recent_high = recent['最高'].astype(float).tolist()
                            recent_low = recent['最低'].astype(float).tolist()
                            recent_ret = (recent['收盘'].pct_change() * 100).fillna(0).astype(float).tolist()
                            vol = float(recent['收盘'].pct_change().std() * np.sqrt(250)) if len(recent) > 10 else 0.0
                            trend_20 = float((recent['收盘'].iloc[-1] / recent['收盘'].iloc[-20] - 1) * 100) if len(recent) >= 20 else 0.0

                            prompt = f"""你是一位量化交易专家。请根据该股票近60日K线摘要，判断更适合做短线/中线/长线，并给出可执行的策略参数（用于评分阈值交易回测）。

股票：{stock_code} {stock_name}
回测周期：{backtest_period}
当前预测周期（用户选择）：{predict_horizon_days} 天

近60日统计：
- 年化波动率(近似)：{vol:.3f}
- 近20日涨跌幅(%)：{trend_20:.2f}

近60日序列（同长度）：
dates: {','.join([d.strftime('%Y-%m-%d') for d in recent_dates])}
open: {recent_open}
high: {recent_high}
low: {recent_low}
close: {recent_close}
ret_pct: {recent_ret}

请只输出严格JSON（不要Markdown），字段如下：
{{
  "horizon": "短线" | "中线" | "长线",
  "strategy_horizon_days": 3-30 的整数（建议持有天数，允许不同于用户预测周期）,
  "take_profit": 1-25 的数字（止盈百分比）,
  "stop_loss": -25 到 -1 的数字（止损百分比，必须为负数）,
  "buy_threshold": 0-100 的数字,
  "sell_threshold": 0-100 的数字（必须小于 buy_threshold）,
  "notes": "一句话说明为什么这样设"
}}
"""
                            deepseek_api_key = user_config.get_deepseek_api_key()
                            analyzer = get_deepseek_analyzer(deepseek_api_key)
                            raw = analyzer._call_deepseek_api(prompt)
                            try:
                                ai_params = json.loads(raw)
                            except Exception:
                                ai_params = None

                            st.session_state['ai_param_cache'][param_key] = ai_params

                    if isinstance(ai_params, dict):
                        try:
                            strategy_horizon_days = int(ai_params.get('strategy_horizon_days', strategy_horizon_days))
                            strategy_horizon_days = max(1, min(60, strategy_horizon_days))

                            tp = float(ai_params.get('take_profit', take_profit))
                            sl = float(ai_params.get('stop_loss', stop_loss))
                            take_profit = max(0.5, min(50.0, tp))
                            stop_loss = -abs(sl)

                            bt = float(ai_params.get('buy_threshold', buy_threshold))
                            stt = float(ai_params.get('sell_threshold', sell_threshold))
                            if bt <= stt:
                                stt = min(stt, bt - 1)
                            buy_threshold = max(0.0, min(100.0, bt))
                            sell_threshold = max(0.0, min(100.0, stt))

                            st.success(f"🤖 DeepSeek策略建议：{ai_params.get('horizon','')}（持有≈{strategy_horizon_days}天，止盈{take_profit:.1f}%，止损{stop_loss:.1f}%）")
                            st.info(str(ai_params.get('notes', '')).strip())
                        except Exception:
                            pass

                if "backtest_override_hold_days" in st.session_state:
                    strategy_horizon_days = int(st.session_state["backtest_override_hold_days"])
                    strategy_horizon_days = max(1, min(60, strategy_horizon_days))

                if "backtest_override_sell_threshold" in st.session_state:
                    sell_threshold = float(st.session_state["backtest_override_sell_threshold"])
                if "backtest_override_take_profit" in st.session_state:
                    take_profit = float(st.session_state["backtest_override_take_profit"])
                if "backtest_override_stop_loss" in st.session_state:
                    stop_loss = float(st.session_state["backtest_override_stop_loss"])
                
                st.markdown("### ✅ 本次回测生效参数")
                col_p1, col_p2, col_p3, col_p4 = st.columns(4)
                with col_p1:
                    st.metric("生效买入阈值", f"{buy_threshold:.2f}")
                with col_p2:
                    st.metric("生效卖出阈值", f"{sell_threshold:.2f}")
                with col_p3:
                    st.metric("止盈(%)", f"{take_profit:.1f}")
                with col_p4:
                    st.metric("止损(%)", f"{stop_loss:.1f}")
                st.metric("策略持有天数", int(strategy_horizon_days))
                
                # 遍历历史数据，计算评分并进行回测
                st.info(f"🔄 开始回测分析，使用 {len(kline_data)} 条历史数据，预测周期为 {predict_horizon_days} 天（策略持有≈{strategy_horizon_days}天）")
                
                show_debug = st.checkbox("显示调试信息", value=False)
                show_trade_logs = st.checkbox("显示交易过程提示", value=False)
                
                # 记录评分分布
                scores = []
                eval_records = []
                rule_stats = {
                    'triggered': {},
                    'unrecognized': {},
                    'recognized_not_triggered': {}
                }
                
                horizon_for_loop = max(predict_horizon_days, int(strategy_horizon_days))
                for i in range(len(kline_data) - horizon_for_loop):
                    # 获取当前日期的数据
                    current_data = kline_data.iloc[i:i+30]  # 使用30天数据计算指标
                    
                    if len(current_data) < 30:
                        continue
                    
                    score, score_detail = scorer.calculate_score_detail(current_data)
                    scores.append(score)
                    
                    for item in score_detail.get('triggered_items', []):
                        key = f"{item.get('section')} | {item.get('condition')}"
                        rule_stats['triggered'][key] = rule_stats['triggered'].get(key, 0) + 1
                    for item in score_detail.get('unrecognized_items', []):
                        key = f"{item.get('section')} | {item.get('condition')}"
                        rule_stats['unrecognized'][key] = rule_stats['unrecognized'].get(key, 0) + 1
                    for item in score_detail.get('recognized_not_triggered_items', []):
                        key = f"{item.get('section')} | {item.get('condition')}"
                        rule_stats['recognized_not_triggered'][key] = rule_stats['recognized_not_triggered'].get(key, 0) + 1
                    
                    # 当前价格
                    current_price = current_data['收盘'].iloc[-1]
                    current_date = current_data.index[-1]

                    future_loc = i + 29 + predict_horizon_days
                    if future_loc < len(kline_data) and current_price:
                        future_price = kline_data['收盘'].iloc[future_loc]
                        forward_return = (future_price - current_price) / current_price * 100
                        eval_records.append({
                            "日期": current_date,
                            "评分": score,
                            "未来收益": forward_return
                        })
                    
                    # 调试信息：显示评分
                    if i % 20 == 0:  # 每20个周期显示一次，避免信息过多
                        if show_debug:
                            st.info(f"调试：日期 {current_date.strftime('%Y-%m-%d')}，评分为 {score:.2f}，买入阈值为 {buy_threshold}")
                    
                    # 中短线交易逻辑
                    signal = "观望"
                    actual_return = 0
                    
                    if not holding:
                        # 未持仓状态 - 考虑买入
                        if score > buy_threshold:
                            entry_stats['score_above_buy'] += 1
                            # T+1交易机制：使用次日开盘价成交
                            # 检查是否有次日数据
                            # 注意：current_data = kline_data.iloc[i:i+30]，最后一天索引是 i+29
                            # 所以次日索引应该是 i+30
                            if i + 30 < len(kline_data):
                                next_day_data = kline_data.iloc[i + 30]
                                next_open = next_day_data['开盘']
                                next_date = next_day_data['日期'] if '日期' in next_day_data else kline_data.index[i + 30]
                                
                                # 主要策略：价格处于相对低位且评分高于买入阈值
                                if len(current_data) >= 30:
                                    recent_low = current_data['收盘'].tail(30).min()
                                    price_to_low = (current_price - recent_low) / recent_low * 100
                                    # 放宽价格位置检查条件
                                    if price_to_low < 25:  # 价格距离近期低点不超过25%
                                        # 买入信号
                                        signal = "买入-低买策略"
                                        holding = True
                                        entry_price = next_open  # 使用次日开盘价
                                        entry_date = next_date
                                        holding_days = 0
                                        entry_stats['executed_buys'] += 1
                                        # 计算买入数量（假设全仓买入）
                                        buy_quantity = current_capital / entry_price
                                        trades.append({
                                            'date': entry_date,
                                            'signal': signal,
                                            'price': entry_price,
                                            'return': 0.0,
                                            'capital': current_capital
                                        })
                                        if show_trade_logs:
                                            st.success(f"产生买入信号（次日成交）：日期 {entry_date.strftime('%Y-%m-%d')}，评分 {score:.2f}，成交价 {entry_price:.2f} 元")
                                    else:
                                        # 备用方案：即使价格位置不满足条件，只要评分足够高，仍然产生买入信号
                                        if score > buy_threshold + 10:  # 评分高于买入阈值10分以上
                                            signal = "买入-高评分策略"
                                            holding = True
                                            entry_price = next_open  # 使用次日开盘价
                                            entry_date = next_date
                                            holding_days = 0
                                            entry_stats['executed_buys'] += 1
                                            trades.append({
                                                'date': entry_date,
                                                'signal': signal,
                                                'price': entry_price,
                                                'return': 0.0,
                                                'capital': current_capital
                                            })
                                            if show_trade_logs:
                                                st.success(f"产生买入信号（次日成交）：日期 {entry_date.strftime('%Y-%m-%d')}，评分 {score:.2f}，成交价 {entry_price:.2f} 元")
                                        else:
                                            entry_stats['price_filter_blocked'] += 1
                                else:
                                    # 数据不足时的备用方案
                                    signal = "买入-数据不足"
                                    holding = True
                                    entry_price = next_open  # 使用次日开盘价
                                    entry_date = next_date
                                    holding_days = 0
                                    entry_stats['executed_buys'] += 1
                                    trades.append({
                                        'date': entry_date,
                                        'signal': signal,
                                        'price': entry_price,
                                        'return': 0.0,
                                        'capital': current_capital
                                    })
                                    if show_trade_logs:
                                        st.success(f"产生买入信号（次日成交）：日期 {entry_date.strftime('%Y-%m-%d')}，评分 {score:.2f}，成交价 {entry_price:.2f} 元")
                    else:
                        # 持仓状态 - 考虑卖出
                        holding_days += 1
                        
                        # 检查是否有次日数据用于卖出
                        # 注意：current_data = kline_data.iloc[i:i+30]，最后一天索引是 i+29
                        # 所以次日索引应该是 i+30
                        if i + 30 < len(kline_data):
                            next_day_data = kline_data.iloc[i + 30]
                            next_open = next_day_data['开盘']
                            next_date = next_day_data['日期'] if '日期' in next_day_data else kline_data.index[i + 30]
                            
                            # 计算基于次日开盘价的潜在收益（用于止盈止损判断）
                            # 注意：这里我们假设在次日开盘时根据前一日收盘后的决策进行交易
                            # 但止盈止损通常是在盘中触发，这里简化为收盘价判断，次日开盘执行
                            # 或者：基于当日收盘价判断信号，次日开盘执行卖出
                            
                            # 基于当日收盘价计算当前浮动收益，决定是否在次日开盘卖出
                            current_return = (current_price - entry_price) / entry_price * 100
                            
                            sell_signal_triggered = False
                            sell_reason = ""
                            
                            # 高卖策略：达到止盈、止损或持有天数上限
                            if current_return >= take_profit:
                                sell_signal_triggered = True
                                sell_reason = "卖出-止盈"
                            elif current_return <= stop_loss:
                                sell_signal_triggered = True
                                sell_reason = "卖出-止损"
                            elif holding_days >= int(strategy_horizon_days):
                                sell_signal_triggered = True
                                sell_reason = "卖出-持有到期"
                            elif score < sell_threshold:
                                sell_signal_triggered = True
                                sell_reason = "卖出-评分下降"
                            
                            if sell_signal_triggered:
                                signal = sell_reason
                                holding = False
                                # 使用次日开盘价卖出
                                exit_price = next_open
                                actual_return = (exit_price - entry_price) / entry_price * 100
                                cumulative_return += actual_return
                                # 更新资金
                                current_capital = current_capital * (1 + actual_return / 100)
                                max_capital = max(max_capital, current_capital)
                                min_capital = min(min_capital, current_capital)
                                capital_curve.append({'日期': next_date, '资金': float(current_capital)})
                                # 记录交易
                                trades.append({
                                    'date': next_date,  # 记录实际成交日期
                                    'signal': signal,
                                    'price': exit_price,
                                    'return': actual_return,
                                    'capital': current_capital
                                })
                                if show_trade_logs:
                                    st.info(f"产生卖出信号（次日成交）：日期 {next_date.strftime('%Y-%m-%d')}，原因 {signal}，成交价 {exit_price:.2f}，收益率 {actual_return:.2f}%")
                    
                    # 存储回测结果
                    if signal != "观望":
                        backtest_results.append({
                            "日期": current_date.strftime('%Y-%m-%d'),
                            "评分": round(score, 2),
                            "信号": signal,
                            "实际收益": round(actual_return, 2),
                            "持有天数": holding_days if holding else 0,
                            "买入价格": round(entry_price, 2) if entry_price > 0 else 0,
                            "卖出价格": round(current_price, 2) if signal.startswith("卖出") else 0
                        })
                    
                    # 重置买入价格和日期（如果卖出）
                    if signal.startswith("卖出"):
                        entry_price = 0
                        entry_date = None
                        holding_days = 0
                
                st.caption("Backtest UI build: 2026-02-14")
                
                if eval_records:
                    with st.expander("查看评分预测力验证（不含交易规则）", expanded=True):
                        eval_df = pd.DataFrame(eval_records)
                        eval_df['日期'] = pd.to_datetime(eval_df['日期'])
                        eval_df = eval_df.sort_values('日期').reset_index(drop=True)
                        if len(eval_df) >= 30:
                            ic = eval_df['评分'].corr(eval_df['未来收益'])
                            rank_ic = eval_df['评分'].rank().corr(eval_df['未来收益'].rank())
                            col_ic, col_ric, col_n = st.columns(3)
                            with col_ic:
                                st.metric("IC(相关系数)", f"{ic:.3f}" if ic == ic else "N/A")
                            with col_ric:
                                st.metric("RankIC(秩相关)", f"{rank_ic:.3f}" if rank_ic == rank_ic else "N/A")
                            with col_n:
                                st.metric("样本数", len(eval_df))

                            eval_df['分组'] = pd.qcut(eval_df['评分'], 5, labels=['Q1(低)', 'Q2', 'Q3', 'Q4', 'Q5(高)'], duplicates='drop')
                            group_stats = eval_df.groupby('分组', observed=True).agg(
                                样本数=('未来收益', 'count'),
                                平均未来收益=('未来收益', 'mean'),
                                胜率=('未来收益', lambda x: (x > 0).mean() * 100),
                            ).reset_index()
                            st.dataframe(group_stats, use_container_width=True)
                        else:
                            st.info("⚠️ 样本不足（<30），无法进行稳定的相关性/分组检验")
                    
                    with st.expander("阈值自动寻优（基于评分→未来收益，不依赖交易规则）", expanded=False):
                        if len(eval_df) < 30:
                            st.info("⚠️ 样本不足（<30），暂不做阈值寻优")
                        else:
                            scores_series = eval_df['评分']
                            candidate_thresholds = sorted(set([float(scores_series.quantile(q)) for q in np.linspace(0.5, 0.98, 25)]))
                            rows = []
                            for th in candidate_thresholds:
                                subset = eval_df[eval_df['评分'] >= th]
                                if len(subset) < 20:
                                    continue
                                win = (subset['未来收益'] > 0).mean() * 100
                                avg = subset['未来收益'].mean()
                                rows.append({
                                    '候选买入阈值': round(th, 2),
                                    '样本数': int(len(subset)),
                                    '胜率(未来收益>0)': round(win, 2),
                                    '平均未来收益(%)': round(avg, 3),
                                    '目标值': round(avg * (win / 100), 4),
                                })

                            if rows:
                                opt_df = pd.DataFrame(rows).sort_values(['目标值', '平均未来收益(%)', '胜率(未来收益>0)'], ascending=False)
                                best = opt_df.iloc[0].to_dict()
                                st.dataframe(opt_df.head(15), use_container_width=True)
                                st.success(f"推荐买入阈值≈{best['候选买入阈值']}（样本{best['样本数']}，胜率{best['胜率(未来收益>0)']}%，平均未来收益{best['平均未来收益(%)']}%）")

                                if st.button("应用推荐买入阈值（下次回测生效）"):
                                    st.session_state['backtest_override_buy_threshold'] = float(best['候选买入阈值'])
                                    st.success("✅ 已保存推荐买入阈值：重新点击“开始回测”即可生效")
                            else:
                                st.info("⚠️ 未找到满足样本数要求的阈值候选")
                else:
                    st.info("⚠️ 评分预测力验证数据为空")

                with st.expander("查看公式执行情况（与评分直接相关）", expanded=True):
                    import pandas as pd

                    triggered = pd.DataFrame(
                        [{'规则': k, '触发次数': v} for k, v in sorted(rule_stats['triggered'].items(), key=lambda x: x[1], reverse=True)[:20]]
                    )
                    unrecognized = pd.DataFrame(
                        [{'规则': k, '出现次数': v} for k, v in sorted(rule_stats['unrecognized'].items(), key=lambda x: x[1], reverse=True)[:20]]
                    )
                    recognized_not_triggered = pd.DataFrame(
                        [{'规则': k, '出现次数': v} for k, v in sorted(rule_stats['recognized_not_triggered'].items(), key=lambda x: x[1], reverse=True)[:20]]
                    )

                    col_a, col_b, col_c = st.columns(3)
                    with col_a:
                        st.metric("触发规则数", len(rule_stats['triggered']))
                        st.dataframe(triggered, use_container_width=True, height=300)
                    with col_b:
                        st.metric("无法识别规则数", len(rule_stats['unrecognized']))
                        st.dataframe(unrecognized, use_container_width=True, height=300)
                    with col_c:
                        st.metric("识别但未触发规则数", len(rule_stats['recognized_not_triggered']))
                        st.dataframe(recognized_not_triggered, use_container_width=True, height=300)

                    if len(rule_stats['unrecognized']) > 0:
                        st.warning("存在“无法识别规则”，说明AI公式的自然语言表述与评分器支持的条件不匹配，会导致评分偏低/买入阈值永远达不到。建议改为结构化JSON规则或扩展评分器的规则解析。")

                with st.expander("🔁 自动迭代优化（Walk-forward 验证集）", expanded=False):
                    st.info("目标：在不引入未来函数/过拟合的前提下，提高验证集“未来收益>0”的胜率与平均未来收益。")

                    auto_optimize_enabled = st.checkbox("回测后自动迭代优化（将调用DeepSeek API）", value=True)
                    max_iters = st.slider("最大迭代轮数", 1, 8, 3, 1)
                    train_ratio = st.slider("训练集比例（时间顺序切分）", 0.5, 0.9, 0.7, 0.05)
                    target_win_rate = st.slider("目标验证集胜率(%)", 50.0, 70.0, 55.0, 0.5)
                    min_val_samples = st.slider("验证集最小样本数", 20, 120, 30, 5)

                    supported_conditions = [
                        "MA5 > MA20",
                        "MA10 > MA30",
                        "均线多头排列（MA5>MA10>MA20）",
                        "近期5日涨幅 > 3%",
                        "MACD金叉且柱状图扩大",
                        "KDJ（K>D且在20-80区间）",
                        "收盘价突破布林带中轨",
                        "成交量 > 20日均量1.3倍",
                        "量比（当日/5日均量）>1.2",
                        "10日波动率 < 近期30日波动率",
                        "价格处于20日均线上方且偏离度<8%",
                        "RSI在40-60之间",
                        "扣分：长上影线",
                        "扣分：涨幅>5%但波动率同步放大",
                        "扣分：价涨量缩",
                        "扣分：RSI>70或<30",
                        "市场环境：近20日上涨趋势（涨幅>3%）否则低分",
                    ]

                    def _select_threshold(df: pd.DataFrame):
                        scores_series = df['评分']
                        candidate_thresholds = sorted(set([float(scores_series.quantile(q)) for q in np.linspace(0.5, 0.98, 25)]))
                        best = None
                        for th in candidate_thresholds:
                            subset = df[df['评分'] >= th]
                            if len(subset) < max(20, int(min_val_samples)):
                                continue
                            win = (subset['未来收益'] > 0).mean() * 100
                            avg = subset['未来收益'].mean()
                            obj = avg * (win / 100)
                            row = {
                                'threshold': float(th),
                                'samples': int(len(subset)),
                                'win_rate': float(win),
                                'avg_return': float(avg),
                                'objective': float(obj),
                            }
                            if best is None or row['objective'] > best['objective']:
                                best = row
                        return best

                    def _eval_hit(df: pd.DataFrame, threshold: float):
                        subset = df[df['评分'] >= threshold]
                        if len(subset) == 0:
                            return {'samples': 0, 'win_rate': 0.0, 'avg_return': 0.0}
                        win = (subset['未来收益'] > 0).mean() * 100
                        avg = subset['未来收益'].mean()
                        return {'samples': int(len(subset)), 'win_rate': float(win), 'avg_return': float(avg)}

                    def _summarize_rules():
                        top_unrec = sorted(rule_stats['unrecognized'].items(), key=lambda x: x[1], reverse=True)[:10]
                        top_trigger = sorted(rule_stats['triggered'].items(), key=lambda x: x[1], reverse=True)[:10]
                        return top_trigger, top_unrec

                    def _simulate_equity_curve(df: pd.DataFrame, threshold: float, holding_days: int):
                        df2 = df.sort_values('日期').reset_index(drop=True)
                        capital = 1.0
                        points = []
                        i = 0
                        while i < len(df2):
                            row = df2.iloc[i]
                            if float(row['评分']) >= float(threshold):
                                capital *= (1 + float(row['未来收益']) / 100)
                                points.append((row['日期'], capital))
                                i += max(1, int(holding_days))
                            else:
                                i += 1
                        if not points:
                            return pd.Series(dtype=float)
                        s = pd.Series([p[1] for p in points], index=pd.to_datetime([p[0] for p in points]))
                        s = s.sort_index()
                        return s

                    import hashlib
                    cache_key = hashlib.md5(
                        (str(stock_code) + str(backtest_period) + str(predict_period) + str(max_iters) + str(train_ratio) + str(min_val_samples) + str(formula_text)).encode('utf-8')
                    ).hexdigest()
                    if 'auto_optimize_cache' not in st.session_state:
                        st.session_state['auto_optimize_cache'] = {}

                    if auto_optimize_enabled:
                        if not user_config.get_deepseek_api_key():
                            st.error("❌ 未配置DeepSeek API密钥，无法自动迭代优化")
                        elif not eval_records:
                            st.error("❌ 无预测力数据，无法优化")
                        else:
                            ignore_cache = st.checkbox("忽略缓存，强制重新迭代", value=False)
                            if st.button("清除此股票本次优化缓存"):
                                if cache_key in st.session_state['auto_optimize_cache']:
                                    del st.session_state['auto_optimize_cache'][cache_key]
                                    st.success("✅ 已清除缓存，请重新运行回测")

                            cached = None if ignore_cache else st.session_state['auto_optimize_cache'].get(cache_key)
                            if cached is None:
                                with st.spinner("正在自动迭代优化并评估验证集..."):
                                    from deepseek_analyzer import get_deepseek_analyzer
                                    analyzer = get_deepseek_analyzer(user_config.get_deepseek_api_key())

                                    history = []
                                    current_formula_text = formula_text
                                    best_formula_text = formula_text
                                    best_val_objective = None
                                    best_buy_threshold = None
                                    best_val_total_return = None
                                    same_formula_streak = 0

                                    base_df = pd.DataFrame(eval_records)
                                    base_df['日期'] = pd.to_datetime(base_df['日期'])
                                    base_df = base_df.sort_values('日期').reset_index(drop=True)
                                    n0 = len(base_df)
                                    split0 = int(n0 * train_ratio)
                                    train0 = base_df.iloc[:split0].copy()
                                    val0 = base_df.iloc[split0:].copy()
                                    th0 = _select_threshold(train0) if len(train0) >= 30 else None
                                    if th0 is not None:
                                        val_hit0 = _eval_hit(val0, th0['threshold'])
                                        eq0 = _simulate_equity_curve(val0, th0['threshold'], max_holding_days)
                                        val_total_return0 = (float(eq0.iloc[-1]) - 1) * 100 if len(eq0) else 0.0
                                        base_hash = hashlib.md5(current_formula_text.encode('utf-8')).hexdigest()[:8]
                                        history.append({
                                            '迭代': 0,
                                            '公式哈希': base_hash,
                                            '推荐买入阈值': round(float(th0['threshold']), 2),
                                            '验证样本数': val_hit0['samples'],
                                            '验证胜率%': round(val_hit0['win_rate'], 2),
                                            '验证平均未来收益%': round(val_hit0['avg_return'], 3),
                                            '验证累计收益%': round(val_total_return0, 3),
                                        })

                                    for it in range(1, max_iters + 1):
                                        local_formula_info = parser.parse_deepseek_result(current_formula_text)
                                        local_scorer = DynamicScorer(local_formula_info)

                                        local_eval_records = []
                                        for j in range(len(kline_data) - max_holding_days):
                                            w = kline_data.iloc[j:j+30]
                                            if len(w) < 30:
                                                continue
                                            s, _d = local_scorer.calculate_score_detail(w)
                                            price = w['收盘'].iloc[-1]
                                            dt = w.index[-1]
                                            future_loc = j + 29 + max_holding_days
                                            if future_loc < len(kline_data) and price:
                                                future_price = kline_data['收盘'].iloc[future_loc]
                                                fr = (future_price - price) / price * 100
                                                local_eval_records.append({'日期': dt, '评分': s, '未来收益': fr})

                                        local_eval_df = pd.DataFrame(local_eval_records)
                                        local_eval_df['日期'] = pd.to_datetime(local_eval_df['日期'])
                                        local_eval_df = local_eval_df.sort_values('日期').reset_index(drop=True)

                                        n = len(local_eval_df)
                                        split = int(n * train_ratio)
                                        train_df = local_eval_df.iloc[:split].copy()
                                        val_df = local_eval_df.iloc[split:].copy()

                                        train_rank_ic = train_df['评分'].rank().corr(train_df['未来收益'].rank()) if len(train_df) >= 30 else np.nan
                                        val_rank_ic = val_df['评分'].rank().corr(val_df['未来收益'].rank()) if len(val_df) >= 30 else np.nan

                                        best_th = _select_threshold(train_df) if len(train_df) >= 30 else None
                                        if best_th is None:
                                            break

                                        val_hit = _eval_hit(val_df, best_th['threshold'])
                                        rec_buy = float(best_th['threshold'])
                                        obj = val_hit['avg_return'] * (val_hit['win_rate'] / 100)
                                        eq = _simulate_equity_curve(val_df, rec_buy, max_holding_days)
                                        val_total_return = (float(eq.iloc[-1]) - 1) * 100 if len(eq) else 0.0
                                        cur_hash = hashlib.md5(current_formula_text.encode('utf-8')).hexdigest()[:8]

                                        history.append({
                                            '迭代': it,
                                            '公式哈希': cur_hash,
                                            '训练RankIC': None if train_rank_ic != train_rank_ic else round(float(train_rank_ic), 3),
                                            '验证RankIC': None if val_rank_ic != val_rank_ic else round(float(val_rank_ic), 3),
                                            '推荐买入阈值': round(rec_buy, 2),
                                            '验证样本数': val_hit['samples'],
                                            '验证胜率%': round(val_hit['win_rate'], 2),
                                            '验证平均未来收益%': round(val_hit['avg_return'], 3),
                                            '验证目标值': round(obj, 4),
                                            '验证累计收益%': round(val_total_return, 3),
                                        })

                                        if val_hit['samples'] >= min_val_samples:
                                            if best_val_objective is None or obj > best_val_objective:
                                                best_val_objective = obj
                                                best_val_total_return = val_total_return
                                                best_formula_text = current_formula_text
                                                best_buy_threshold = rec_buy

                                        if val_hit['samples'] >= min_val_samples and val_hit['win_rate'] >= target_win_rate and val_hit['avg_return'] > 0:
                                            break

                                        top_trigger, top_unrec = _summarize_rules()
                                        unrec_text = "\n".join([f"- {k}（{v}次）" for k, v in top_unrec]) if top_unrec else "- 无"
                                        trig_text = "\n".join([f"- {k}（{v}次）" for k, v in top_trigger]) if top_trigger else "- 无"

                                        prompt = f"""你是一位专业的量化交易专家。

我们在做“评分公式→未来{max_holding_days}天收益”的预测力检验，并采用时间顺序切分的 walk-forward 方式：前{int(train_ratio*100)}%为训练集，后{int((1-train_ratio)*100)}%为验证集。

当前公式（需要你基于反馈做一版改进）：
{current_formula_text}

回测反馈（以验证集为准）：
- 推荐买入阈值≈{rec_buy:.2f}
- 验证集样本数={val_hit['samples']}
- 验证集胜率(未来收益>0)={val_hit['win_rate']:.2f}%
- 验证集平均未来收益={val_hit['avg_return']:.3f}%
- 训练RankIC={train_rank_ic if train_rank_ic==train_rank_ic else 'N/A'}，验证RankIC={val_rank_ic if val_rank_ic==val_rank_ic else 'N/A'}

规则执行情况摘要：
- 最常触发规则：
{trig_text}
- 无法识别的规则（请避免这些表述，或改写为可执行的条件）：
{unrec_text}

你的任务：
1) 生成一版“更可执行、更稳定”的评分公式（满分100分），并自主给出买入阈值与卖出阈值；
2) 目标是在验证集提升胜率，并保持平均未来收益为正；
3) 必须只使用以下可执行条件/扣分项（否则会被系统判定为无法识别）：
{chr(10).join([f'- {c}' for c in supported_conditions])}
4) 权重总和=100，阈值0-100且买入阈值>卖出阈值。

请严格按如下格式输出（保持和当前系统解析器兼容）：
评分公式设计
 趋势强度（30分）：
- ...（10分）
- ...（10分）
- ...（10分）
 动量确认（25分）：
- ...（10分）
- ...（10分）
- ...（5分）
 量价配合（20分）：
- ...（10分）
- ...（10分）
 风险控制（15分）：
- ...（5分）
- ...（5分）
- ...（5分）
 市场环境适配（10分）：
- ...（10分）
 扣分项（直接从总分扣除）：
- ...（扣5分）
- ...（扣3分）
- ...（扣5分）
最优阈值分析
 买入阈值：值：xx
 卖出阈值：值：xx
"""
                                        new_formula = analyzer._call_deepseek_api(prompt)
                                        if not new_formula or len(new_formula.strip()) < 50:
                                            break
                                        new_hash = hashlib.md5(new_formula.encode('utf-8')).hexdigest()
                                        old_hash = hashlib.md5(current_formula_text.encode('utf-8')).hexdigest()
                                        if new_hash == old_hash:
                                            same_formula_streak += 1
                                        else:
                                            same_formula_streak = 0
                                        current_formula_text = new_formula
                                        if same_formula_streak >= 2:
                                            break

                                    cached = {
                                        'history': history,
                                        'best_formula_text': best_formula_text,
                                        'best_buy_threshold': best_buy_threshold,
                                        'best_val_total_return': best_val_total_return,
                                    }
                                    st.session_state['auto_optimize_cache'][cache_key] = cached

                            if cached and cached.get('history'):
                                hist_df = pd.DataFrame(cached['history'])
                                st.dataframe(hist_df, use_container_width=True)

                                chart_df = hist_df[['迭代', '验证累计收益%']].dropna()
                                if not chart_df.empty:
                                    chart_df = chart_df.set_index('迭代')
                                    st.line_chart(chart_df, width='stretch')

                                if cached.get('best_formula_text') and cached.get('best_buy_threshold') is not None:
                                    st.session_state['ai_optimized_formula'] = cached['best_formula_text']
                                    st.session_state['backtest_override_buy_threshold'] = float(cached['best_buy_threshold'])

                                    base_df = pd.DataFrame(eval_records)
                                    base_df['日期'] = pd.to_datetime(base_df['日期'])
                                    base_df = base_df.sort_values('日期').reset_index(drop=True)
                                    n0 = len(base_df)
                                    split0 = int(n0 * train_ratio)
                                    train0 = base_df.iloc[:split0].copy()
                                    th0 = _select_threshold(train0) if len(train0) >= 30 else None
                                    if th0 is not None:
                                        eq_before = _simulate_equity_curve(base_df, float(th0['threshold']), max_holding_days)
                                    else:
                                        eq_before = pd.Series(dtype=float)

                                    best_info = parser.parse_deepseek_result(cached['best_formula_text'])
                                    best_scorer = DynamicScorer(best_info)
                                    best_eval_records = []
                                    for j in range(len(kline_data) - max_holding_days):
                                        w = kline_data.iloc[j:j+30]
                                        if len(w) < 30:
                                            continue
                                        s, _d = best_scorer.calculate_score_detail(w)
                                        price = w['收盘'].iloc[-1]
                                        dt = w.index[-1]
                                        future_loc = j + 29 + max_holding_days
                                        if future_loc < len(kline_data) and price:
                                            future_price = kline_data['收盘'].iloc[future_loc]
                                            fr = (future_price - price) / price * 100
                                            best_eval_records.append({'日期': dt, '评分': s, '未来收益': fr})
                                    best_df = pd.DataFrame(best_eval_records)
                                    best_df['日期'] = pd.to_datetime(best_df['日期'])
                                    best_df = best_df.sort_values('日期').reset_index(drop=True)
                                    eq_after = _simulate_equity_curve(best_df, float(cached['best_buy_threshold']), max_holding_days)

                                    if len(eq_before) or len(eq_after):
                                        merged = pd.DataFrame({'优化前': eq_before, '优化后': eq_after})
                                        merged = merged.sort_index().fillna(method='ffill')
                                        st.line_chart(merged, width='stretch')
                                    st.expander("查看最终采用的公式").markdown(cached['best_formula_text'])
                
                # 3. 计算绩效指标
                if backtest_results:
                    backtest_df = pd.DataFrame(backtest_results)
                    
                    # 计算胜率 - 只考虑卖出信号，因为只有卖出时才会产生实际收益
                    sell_signals = backtest_df[backtest_df['信号'].str.startswith("卖出")]
                    if len(sell_signals) > 0:
                        winning_trades = sell_signals[sell_signals['实际收益'] > 0]
                        win_rate = len(winning_trades) / len(sell_signals) * 100
                        avg_return = sell_signals['实际收益'].mean()
                        total_trades = len(sell_signals)
                        profitable_trades = len(winning_trades)
                    else:
                        win_rate = 0
                        avg_return = 0
                        total_trades = 0
                        profitable_trades = 0
                    
                    # 显示交易信号分布
                    st.expander("查看交易信号分布").dataframe(backtest_df['信号'].value_counts())
                    
                    # 显示评分分布
                    if scores:
                        with st.expander("查看评分分布"):
                            st.markdown("### 📊 评分分布统计")
                            st.metric("平均评分", f"{sum(scores)/len(scores):.2f}")
                            st.metric("最高评分", f"{max(scores):.2f}")
                            st.metric("最低评分", f"{min(scores):.2f}")
                            
                            # 绘制评分分布直方图
                            import matplotlib.pyplot as plt
                            import numpy as np
                            
                            plt.figure(figsize=(10, 6))
                            plt.hist(scores, bins=20, alpha=0.7, color='blue', edgecolor='black')
                            plt.axvline(buy_threshold, color='green', linestyle='dashed', linewidth=2, label=f'买入阈值: {buy_threshold}')
                            plt.axvline(sell_threshold, color='red', linestyle='dashed', linewidth=2, label=f'卖出阈值: {sell_threshold}')
                            plt.title('评分分布直方图')
                            plt.xlabel('评分')
                            plt.ylabel('频率')
                            plt.legend()
                            plt.grid(axis='y', alpha=0.75)
                            st.pyplot(plt)
                    
                    # 4. 显示回测结果
                    st.success("✅ 回测分析完成")
                    tab_report, tab_diagnosis, tab_opt, tab_export = st.tabs(["📄 报告", "🧪 诊断", "🤖 优化", "📥 导出"])
                    
                    with tab_report:
                        st.markdown("### 📈 策略成效总览")
                        
                        cap_df = pd.DataFrame(capital_curve)
                        cap_df['日期'] = pd.to_datetime(cap_df['日期'])
                        cap_df = cap_df.sort_values('日期').drop_duplicates('日期', keep='last').set_index('日期')
                        cap_df['策略'] = cap_df['资金'] / float(initial_capital)

                        bh_df = kline_data.copy()
                        if '日期' in bh_df.columns:
                            bh_df['日期'] = pd.to_datetime(bh_df['日期'])
                            bh_df = bh_df.set_index('日期')
                        else:
                            bh_df.index = pd.to_datetime(bh_df.index)
                        bh_df = bh_df.sort_index()
                        bh_df['基准'] = bh_df['收盘'] / float(bh_df['收盘'].iloc[0])

                        strat_daily = cap_df['策略'].reindex(bh_df.index).ffill()
                        if len(strat_daily) > 0:
                            strat_daily = strat_daily.fillna(1.0)
                        else:
                            strat_daily = pd.Series(index=bh_df.index, data=1.0)

                        close = bh_df['收盘'].astype(float)
                        ret = close.pct_change().fillna(0.0)
                        ma20 = close.rolling(20).mean()
                        ma60 = close.rolling(60).mean()
                        ma_sig = (ma20 > ma60).astype(float).shift(1).fillna(0.0)
                        ma_equity = (1.0 + ret * ma_sig).cumprod()

                        equity_df = pd.DataFrame({
                            '策略': strat_daily,
                            '基准': bh_df['基准'].astype(float),
                            'MA20/60趋势': ma_equity.astype(float),
                        }).sort_index()
                        equity_df = equity_df.fillna(method='ffill')

                        st.line_chart(equity_df, width='stretch')
                        
                        col_k1, col_k2, col_k3, col_k4 = st.columns(4)
                        with col_k1:
                            st.metric("策略区间收益率", f"{(float(equity_df['策略'].iloc[-1]) - 1) * 100:.2f}%")
                        with col_k2:
                            st.metric("基准区间收益率", f"{(float(equity_df['基准'].iloc[-1]) - 1) * 100:.2f}%")
                        with col_k3:
                            st.metric("实际成交买入次数", entry_stats['executed_buys'])
                        with col_k4:
                            st.metric("评分过阈值次数", entry_stats['score_above_buy'])

                        def _perf_metrics(series: pd.Series):
                            s = series.dropna()
                            if len(s) < 2:
                                return {'总收益率%': 0.0, '年化收益%': 0.0, '夏普': 0.0, '最大回撤%': 0.0}
                            daily = s.pct_change().fillna(0.0)
                            total_ret = (float(s.iloc[-1]) / float(s.iloc[0]) - 1.0) * 100
                            years = max(1e-9, (s.index[-1] - s.index[0]).days / 365.0)
                            ann = (float(s.iloc[-1]) / float(s.iloc[0])) ** (1.0 / years) - 1.0
                            ann_ret = ann * 100
                            vol = float(daily.std() * np.sqrt(250))
                            sharpe = float(daily.mean() / daily.std() * np.sqrt(250)) if daily.std() != 0 else 0.0
                            dd = (s / s.cummax() - 1.0) * 100
                            mdd = float(dd.min())
                            return {'总收益率%': total_ret, '年化收益%': ann_ret, '夏普': sharpe, '最大回撤%': mdd}

                        st.markdown("### 📋 策略对比指标")
                        comp_rows = []
                        for name in ['策略', '基准', 'MA20/60趋势']:
                            m = _perf_metrics(equity_df[name])
                            comp_rows.append({'方案': name, **{k: round(v, 3) for k, v in m.items()}})
                        comp_df = pd.DataFrame(comp_rows)
                        st.dataframe(comp_df, use_container_width=True)

                        dd_df = pd.DataFrame({
                            '策略回撤%': (equity_df['策略'] / equity_df['策略'].cummax() - 1.0) * 100,
                            '基准回撤%': (equity_df['基准'] / equity_df['基准'].cummax() - 1.0) * 100,
                            'MA20/60回撤%': (equity_df['MA20/60趋势'] / equity_df['MA20/60趋势'].cummax() - 1.0) * 100,
                        }, index=equity_df.index)
                        st.markdown("### 📉 回撤曲线（风险对比）")
                        st.line_chart(dd_df, width='stretch')

                        if eval_records:
                            eval_df = pd.DataFrame(eval_records)
                            eval_df['日期'] = pd.to_datetime(eval_df['日期'])
                            eval_df = eval_df.sort_values('日期').reset_index(drop=True)
                            rng = np.random.default_rng(20260214)
                            random_trials = 400
                            target_trades = max(10, int(entry_stats['executed_buys'] or 0))
                            forward = eval_df['未来收益'].values.astype(float)
                            if len(forward) > 30:
                                totals = []
                                for _ in range(random_trials):
                                    idx = rng.choice(len(forward), size=min(target_trades, len(forward)), replace=False)
                                    total = float(np.prod(1 + forward[idx] / 100) - 1) * 100
                                    totals.append(total)
                                totals = np.array(totals)
                                p25, p50, p75 = np.percentile(totals, [25, 50, 75])
                                beat_pct = float((totals < (float(equity_df['策略'].iloc[-1]) - 1) * 100).mean() * 100)
                                st.markdown("### 🧾 参考基线（随机交易者）")
                                col_b1, col_b2, col_b3, col_b4 = st.columns(4)
                                with col_b1:
                                    st.metric("随机交易者收益P50", f"{p50:.2f}%")
                                with col_b2:
                                    st.metric("随机交易者收益P25", f"{p25:.2f}%")
                                with col_b3:
                                    st.metric("随机交易者收益P75", f"{p75:.2f}%")
                                with col_b4:
                                    st.metric("策略击败随机基线(%)", f"{beat_pct:.1f}%")

                        st.markdown("### 🔍 无交易/低收益原因")
                        col_r1, col_r2, col_r3 = st.columns(3)
                        with col_r1:
                            st.metric("价格位置过滤拦截次数", entry_stats['price_filter_blocked'])
                        with col_r2:
                            st.metric("评分未过阈值次数", max(0, int(len(scores) - entry_stats['score_above_buy'])))
                        with col_r3:
                            st.metric("评分序列样本数", len(scores))
                    
                    with tab_diagnosis:
                        st.markdown("### 📊 当前使用的评分公式")
                        st.expander("查看评分公式").markdown(st.session_state.get('ai_optimized_formula') or formula_text)
                        if scores:
                            with st.expander("查看评分分布", expanded=True):
                                st.metric("平均评分", f"{sum(scores)/len(scores):.2f}")
                                st.metric("最高评分", f"{max(scores):.2f}")
                                st.metric("最低评分", f"{min(scores):.2f}")
                                import matplotlib.pyplot as plt
                                plt.figure(figsize=(10, 6))
                                plt.hist(scores, bins=20, alpha=0.7, color='blue', edgecolor='black')
                                plt.axvline(buy_threshold, color='green', linestyle='dashed', linewidth=2, label=f'买入阈值: {buy_threshold}')
                                plt.axvline(sell_threshold, color='red', linestyle='dashed', linewidth=2, label=f'卖出阈值: {sell_threshold}')
                                plt.title('评分分布直方图')
                                plt.xlabel('评分')
                                plt.ylabel('频率')
                                plt.legend()
                                plt.grid(axis='y', alpha=0.75)
                                st.pyplot(plt)
                    
                    with tab_opt:
                        st.markdown("### 🤖 优化与迭代")
                        st.info("阈值寻优、自动迭代优化与AB测结果会显示在本页。")
                        st.markdown("### 🧪 AB测：A求高 vs B求稳（基于完整交易回测）")
                        fee_bps = st.slider("单次交易成本(bps)", 0.0, 50.0, 10.0, 1.0, key="ab_full_fee")
                        train_ratio_ab = st.slider("训练集比例（时间顺序）", 0.5, 0.9, 0.7, 0.05, key="ab_full_train_ratio")
                        min_trades = st.slider("训练段最少交易次数", 2, 50, 8, 1, key="ab_full_min_trades")

                        from abtest_full_engine import optimize_ab_full, optimize_ab_full_walkforward
                        use_walkforward = st.checkbox("使用多折Walk-forward（更稳健）", value=True)
                        if use_walkforward:
                            n_splits = st.slider("折数", 2, 5, 3, 1, key="ab_full_splits")
                            val_ratio = st.slider("每折验证比例", 0.1, 0.35, 0.2, 0.05, key="ab_full_val_ratio")
                            top_k = st.slider("每折TopK候选", 5, 50, 20, 5, key="ab_full_top_k")
                            ab_full = optimize_ab_full_walkforward(
                                kline_data,
                                scorer,
                                n_splits=int(n_splits),
                                val_ratio=float(val_ratio),
                                min_trades=int(min_trades),
                                fee_bps=float(fee_bps),
                                top_k=int(top_k),
                            )
                        else:
                            ab_full = optimize_ab_full(
                                kline_data,
                                scorer,
                                train_ratio=float(train_ratio_ab),
                                min_trades=int(min_trades),
                                fee_bps=float(fee_bps),
                            )

                        a = ab_full.get("A")
                        b = ab_full.get("B")
                        if a is None and b is None:
                            st.info("⚠️ 训练段交易次数不足，无法完成AB测。可降低“训练段最少交易次数”或放宽阈值。")
                        else:
                            rows = []
                            if a is not None:
                                p = a["params"]
                                m = a["val"]
                                rows.append({
                                    "策略": "A(求高)",
                                    "买入阈值": round(p.buy_threshold, 2),
                                    "卖出阈值": round(p.sell_threshold, 2),
                                    "持有天数": int(p.max_holding_days),
                                    "止盈%": round(p.take_profit_pct, 1),
                                    "止损%": round(p.stop_loss_pct, 1),
                                    **{k: round(float(v), 3) for k, v in m.items()},
                                })
                            if b is not None:
                                p = b["params"]
                                m = b["val"]
                                rows.append({
                                    "策略": "B(求稳)",
                                    "买入阈值": round(p.buy_threshold, 2),
                                    "卖出阈值": round(p.sell_threshold, 2),
                                    "持有天数": int(p.max_holding_days),
                                    "止盈%": round(p.take_profit_pct, 1),
                                    "止损%": round(p.stop_loss_pct, 1),
                                    **{k: round(float(v), 3) for k, v in m.items()},
                                })
                            st.dataframe(pd.DataFrame(rows), use_container_width=True)

                            st.markdown("### 🏁 胜出策略（按不同目标）")
                            col_w1, col_w2, col_w3 = st.columns(3)
                            with col_w1:
                                st.metric("求高", ab_full.get("winner_high"))
                            with col_w2:
                                st.metric("求稳", ab_full.get("winner_stable"))
                            with col_w3:
                                st.metric("平衡", ab_full.get("winner_balance"))
                            
                            if use_walkforward and ab_full.get("stability"):
                                st.markdown("### 🧷 稳定性评分（越高越稳）")
                                stab = ab_full["stability"]
                                stab_rows = []
                                for side in ["A", "B"]:
                                    for obj in ["求高", "求稳", "平衡"]:
                                        r = stab.get(side, {}).get(obj, {})
                                        stab_rows.append({
                                            "侧": side,
                                            "目标": obj,
                                            "稳定性": round(float(r.get("stability", 0.0)), 4),
                                            "中位数": round(float(r.get("median", 0.0)), 4),
                                            "波动": round(float(r.get("std", 0.0)), 4),
                                            "折数": int(r.get("n", 0)),
                                        })
                                st.dataframe(pd.DataFrame(stab_rows), use_container_width=True)

                                folds = ab_full.get("folds", [])
                                if folds:
                                    st.markdown("### 🧾 Walk-forward逐折明细")
                                    st.dataframe(pd.DataFrame(folds), use_container_width=True)

                            curve = pd.DataFrame({
                                "A(求高)": (a["val_equity"] if a is not None else pd.Series(dtype=float)),
                                "B(求稳)": (b["val_equity"] if b is not None else pd.Series(dtype=float)),
                            }).sort_index().fillna(method="ffill")
                            st.line_chart(curve, width="stretch")

                            apply_target = st.selectbox("应用哪种目标的胜出策略", ["平衡", "求高", "求稳"], index=0, key="ab_full_apply_target")
                            winner = ab_full.get("winner_balance") if apply_target == "平衡" else (ab_full.get("winner_high") if apply_target == "求高" else ab_full.get("winner_stable"))
                            if st.button("一键应用胜出策略参数（下次回测生效）", key="ab_full_apply"):
                                chosen = a if str(winner) == "A" else b
                                if chosen is not None:
                                    p = chosen["params"]
                                    st.session_state["backtest_override_buy_threshold"] = float(p.buy_threshold)
                                    st.session_state["backtest_override_sell_threshold"] = float(p.sell_threshold)
                                    st.session_state["backtest_override_hold_days"] = int(p.max_holding_days)
                                    st.session_state["backtest_override_take_profit"] = float(p.take_profit_pct)
                                    st.session_state["backtest_override_stop_loss"] = float(p.stop_loss_pct)
                                    st.success(f"✅ 已应用：买入≈{p.buy_threshold:.2f}，卖出≈{p.sell_threshold:.2f}，持有≈{p.max_holding_days}天，止盈{p.take_profit_pct:.1f}%，止损{p.stop_loss_pct:.1f}%（重新点击开始回测生效）")
                    
                    with tab_export:
                        st.markdown("### 📥 导出")
                        st.info("可导出PDF回测报告（包含绩效指标、交易记录与评分模型）。")
                    
                    # 计算专业量化指标
                    if not sell_signals.empty:
                        # 转换收益率为小数
                        returns = sell_signals['实际收益'] / 100
                        # 计算年化收益率 (假设平均持仓天数)
                        avg_holding_days = sell_signals['持有天数'].mean() if '持有天数' in sell_signals.columns else 5
                        annual_return = avg_return / 100 * (250 / avg_holding_days) * win_rate / 100 * 100 # 粗略估算
                        
                        # 计算夏普比率 (假设无风险利率3%)
                        risk_free_rate = 0.03
                        excess_returns = returns - (risk_free_rate / 250 * avg_holding_days)
                        std_dev = returns.std()
                        sharpe_ratio = excess_returns.mean() / std_dev if std_dev != 0 else 0
                        
                        # 计算最大回撤
                        sell_signals_copy = sell_signals.copy()
                        sell_signals_copy['累计收益'] = (1 + returns).cumprod()
                        sell_signals_copy['峰值'] = sell_signals_copy['累计收益'].cummax()
                        sell_signals_copy['回撤'] = (sell_signals_copy['累计收益'] - sell_signals_copy['峰值']) / sell_signals_copy['峰值']
                        max_drawdown = sell_signals_copy['回撤'].min() * 100
                        
                        # 盈亏比
                        avg_win = returns[returns > 0].mean()
                        avg_loss = abs(returns[returns < 0].mean())
                        profit_loss_ratio = avg_win / avg_loss if avg_loss != 0 else float('inf')
                    else:
                        annual_return = 0
                        sharpe_ratio = 0
                        max_drawdown = 0
                        profit_loss_ratio = 0

                    # 准备回测数据用于生成PDF
                    backtest_report_data = {
                        'stock_info': {'symbol': stock_code, 'name': stock_name},
                        'metrics': {
                            'total_trades': total_trades,
                            'profitable_trades': profitable_trades,
                            'win_rate': win_rate,
                            'avg_return': avg_return,
                            'total_return': (current_capital / initial_capital - 1) * 100,
                            'annual_return': annual_return,
                            'sharpe_ratio': sharpe_ratio,
                            'max_drawdown': max_drawdown,
                            'profit_loss_ratio': profit_loss_ratio,
                            'initial_capital': initial_capital,
                            'final_capital': current_capital
                        },
                        'trades': trades,
                        'formula': st.session_state.get('ai_optimized_formula') or formula_text,
                        'equity_curves': equity_df.reset_index().rename(columns={'index': '日期'}).to_dict(orient='records') if 'equity_df' in locals() else None,
                        'comparison_metrics': comp_df.to_dict(orient='records') if 'comp_df' in locals() else None
                    }

                    # 显示导出PDF按钮
                    from pdf_generator_professional import generate_backtest_pdf_report
                    with tab_export:
                        if st.button("📄 生成回测报告 PDF"):
                            with st.spinner("正在生成回测报告 PDF..."):
                                pdf_path = generate_backtest_pdf_report(backtest_report_data)
                                if pdf_path and os.path.exists(pdf_path):
                                    st.success(f"✅ PDF报告生成成功: {pdf_path}")
                                    with open(pdf_path, "rb") as f:
                                        st.download_button(
                                            label="⬇️ 下载回测报告 PDF",
                                            data=f,
                                            file_name=os.path.basename(pdf_path),
                                            mime="application/pdf"
                                        )
                                else:
                                    st.error("❌ 生成PDF报告失败")

                    with tab_report:
                        st.markdown("### 📌 核心绩效指标")
                        col_a, col_b, col_c, col_d = st.columns(4)
                        with col_a:
                            st.metric("总交易次数", total_trades)
                            st.metric("盈亏比", f"{profit_loss_ratio:.2f}")
                        with col_b:
                            st.metric("盈利次数", profitable_trades)
                            st.metric("夏普比率", f"{sharpe_ratio:.2f}")
                        with col_c:
                            st.metric("胜率", f"{win_rate:.1f}%")
                            st.metric("最大回撤", f"{max_drawdown:.2f}%")
                        with col_d:
                            st.metric("平均收益率", f"{avg_return:.2f}%")
                            st.metric("总收益率", f"{(current_capital / initial_capital - 1) * 100:.2f}%")
                    
                    with tab_report:
                        st.markdown("### 📋 交易与信号明细")
                        st.dataframe(backtest_df, use_container_width=True)
                    
                    with tab_report:
                        st.markdown("### 📊 回测绩效图表")
                    
                    # 计算累计收益
                    if not sell_signals.empty:
                        # 复制数据以避免修改原始数据
                        sell_signals_copy = sell_signals.copy()
                        sell_signals_copy['累计收益'] = sell_signals_copy['实际收益'].cumsum()
                        
                        # 绘制累计收益图表
                        performance_df = sell_signals_copy[['日期', '累计收益']]
                        performance_df['日期'] = pd.to_datetime(performance_df['日期'])
                        performance_df = performance_df.set_index('日期')
                        
                        with tab_report:
                            st.line_chart(performance_df, width='stretch')
                    else:
                        with tab_report:
                            st.info("⚠️ 没有卖出信号，无法绘制累计收益图表")
                    
                    # 绘制当日买入卖出操作胜率折线图
                    if not backtest_df.empty:
                        with tab_report:
                            st.markdown("### 📈 当日买入卖出操作胜率折线图")
                        
                        # 按日期分组，计算每日的胜率
                        daily_signals = backtest_df.copy()
                        daily_signals['日期'] = pd.to_datetime(daily_signals['日期'])
                        
                        # 计算每日的买入和卖出信号数量
                        daily_stats = daily_signals.groupby('日期').agg(
                            total_signals=('信号', 'count'),
                            buy_signals=('信号', lambda x: (x.str.startswith('买入')).sum()),
                            sell_signals=('信号', lambda x: (x.str.startswith('卖出')).sum()),
                            profitable_trades=('实际收益', lambda x: (x > 0).sum())
                        ).reset_index()
                        
                        # 计算每日胜率
                        daily_stats['胜率'] = (daily_stats['profitable_trades'] / daily_stats['total_signals'] * 100).fillna(0)
                        
                        # 绘制胜率折线图
                        if not daily_stats.empty:
                            win_rate_df = daily_stats[['日期', '胜率']]
                            win_rate_df = win_rate_df.set_index('日期')
                            with tab_report:
                                st.line_chart(win_rate_df, width='stretch')
                        else:
                            with tab_report:
                                st.info("⚠️ 没有足够的数据，无法绘制胜率折线图")
                    else:
                        with tab_report:
                            st.info("⚠️ 没有回测数据，无法绘制胜率折线图")
                else:
                    st.warning("⚠️ 回测结果为空，请检查参数设置")
            
        except Exception as e:
            st.error(f"❌ 回测失败: {e}")
            st.info("💡 可能的原因：\n1. 股票代码错误\n2. 网络连接问题\n3. 数据源暂时不可用")
            
            # 显示错误详情
            import traceback
            st.code(traceback.format_exc())

st.markdown("---")
footer_html = """
<div class='footer' style='background: linear-gradient(135deg, #F8FAFC 0%, #E0F2FE 100%); padding: 2.5rem 1.5rem; border-radius: 16px; margin-top: 3rem; box-shadow: 0 -2px 16px rgba(0, 0, 0, 0.05);'>
  <div style='text-align: center;'>
    <h3 style='color: #1E40AF; font-size: 1.25rem; font-weight: 700; margin-bottom: 1rem;'>📊 个人选股系统</h3>
    <p style='color: #475569; font-size: 0.95rem; margin-bottom: 0.5rem;'>融合缠论结构 · CPV量价分析 · 基本面筛选 · AI智能分析</p>
    <div style='border-top: 2px solid #E2E8F0; margin: 1.5rem auto; width: 60%;'></div>
    <p style='color: #64748B; font-size: 0.85rem; margin-bottom: 0.25rem;'>⚠️ 风险提示：本系统仅供个人学习研究使用，不构成任何投资建议</p>
    <p style='color: #94A3B8; font-size: 0.8rem;'>© 2026 Personal Stock Selection System · Version 2.0</p>
    <p style='color: #CBD5E1; font-size: 0.75rem; margin-top: 0.5rem;'>Powered by DeepSeek AI · TencentFinance Data · EastMoney API</p>
  </div>
</div>
"""
st.markdown(footer_html, unsafe_allow_html=True)

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

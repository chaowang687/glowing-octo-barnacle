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
from datetime import datetime
from web_designer import get_web_designer

# 页面配置
st.set_page_config(
    page_title="A股量化选股系统",
    page_icon="📈",
    layout="wide",
    initial_sidebar_state="expanded"
)

# 样式设置 - 专业报告风格  
designer = get_web_designer()
professional_css = designer.generate_professional_report_css()
st.markdown(professional_css, unsafe_allow_html=True)

# 主页面内容
current_time = datetime.now().strftime('%Y-%m-%d %H:%M:%S')

col1, col2 = st.columns([3, 1])

with col1:
    st.markdown("# 📊 个人选股系统")
    st.markdown("⚠️ 不构成投资建议")
    st.markdown("📧 作者邮箱：chaowang687@gmail.com")
    st.markdown("融合缠论结构 · CPV量价分析 · 基本面筛选 · AI智能分析")

with col2:
    st.markdown(f"**🕐 {current_time}**")

st.markdown("---")

st.info("💡 请在左侧侧边栏选择功能模块")

st.markdown("""
### 🚀 功能模块

#### 1. 📊 [市场概览](/Market_Overview)
- 查看大盘及行业涨跌幅
- 实时获取个股行情
- 智能筛选热门股票

#### 2. 🤖 [智能综合选股](/Stock_Selection)
- 基于多因子策略选股
- 结合缠论、量价和基本面
- 生成专业选股报告

#### 3. 📈 [评分回测分析](/Backtest)
- 验证评分公式的有效性
- AI 自动优化交易策略
- 历史数据回测
""")

# 页脚
st.markdown("---")
footer_html = f"""
<div class='footer' style='background: linear-gradient(135deg, #F8FAFC 0%, #E0F2FE 100%); padding: 2.5rem 1.5rem; border-radius: 16px; margin-top: 3rem; box-shadow: 0 -2px 16px rgba(0, 0, 0, 0.05);'>
    <div style='text-align: center;'>
        <h3 style='color: #1E40AF; font-size: 1.25rem; font-weight: 700; margin-bottom: 1rem;'>
            📊 个人选股系统
        </h3>
        <p style='color: #475569; font-size: 0.95rem; margin-bottom: 0.5rem;'>
            融合缠论结构 · CPV量价分析 · 基本面筛选 · AI智能分析
        </p>
        <div style='border-top: 2px solid #E2E8F0; margin: 1.5rem auto; width: 60%;'></div>
        <p style='color: #64748B; font-size: 0.85rem; margin-bottom: 0.25rem;'>
            ⚠️ 风险提示：本系统仅供个人学习研究使用，不构成任何投资建议
        </p>
        <p style='color: #94A3B8; font-size: 0.8rem;'>
            © 2026 Personal Stock Selection System · Version 2.0
        </p>
        <p style='color: #CBD5E1; font-size: 0.75rem; margin-top: 0.5rem;'>
            Powered by DeepSeek AI · TencentFinance Data · EastMoney API
        </p>
    </div>
</div>
"""
st.markdown(footer_html, unsafe_allow_html=True)

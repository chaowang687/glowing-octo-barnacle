#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
网页设计工具模块
提供网页设计相关的功能，如颜色方案生成、CSS样式优化等
"""

import random
from typing import Dict, List, Tuple

class WebDesigner:
    """
    网页设计师类
    提供网页设计相关的功能
    """
    
    def __init__(self):
        """
        初始化网页设计师
        """
        self.color_palettes = {
            "dark_mode": {
                "primary": "#FF6B6B",
                "secondary": "#4ADE80",
                "background": "#000000",
                "surface": "#111111",
                "text": "#FFFFFF",
                "border": "#555555"
            },
            "light_mode": {
                "primary": "#FF6B6B",
                "secondary": "#4ADE80",
                "background": "#FFFFFF",
                "surface": "#F5F5F5",
                "text": "#000000",
                "border": "#E0E0E0"
            },
            "modern": {
                "primary": "#6366F1",
                "secondary": "#EC4899",
                "background": "#1F2937",
                "surface": "#374151",
                "text": "#F9FAFB",
                "border": "#4B5563"
            },
            "professional": {
                "primary": "#2563EB",
                "secondary": "#10B981",
                "background": "#F8FAFC",
                "surface": "#FFFFFF",
                "text": "#1E293B",
                "border": "#E2E8F0"
            },
            "professional_report": {
                "primary": "#1E40AF",      # 深蓝色 - 专业、可信
                "secondary": "#D97706",    # 金色 - 财富、价值
                "accent": "#059669",       # 绿色 - 增长、利好
                "danger": "#DC2626",       # 红色 - 风险、利空
                "background": "#F8FAFC",   # 浅灰背景 - 清爽
                "surface": "#FFFFFF",      # 白色卡片
                "surface_dark": "#F1F5F9", # 深色表面
                "text": "#0F172A",         # 深色文字
                "text_light": "#475569",   # 浅色文字
                "border": "#E2E8F0",       # 边框色
                "border_light": "#F1F5F9", # 浅边框
                "success": "#10B981",      # 成功色
                "warning": "#F59E0B",      # 警告色
                "info": "#3B82F6"          # 信息色
            }
        }
    
    def generate_color_palette(self, style: str = "dark_mode") -> Dict[str, str]:
        """
        生成颜色方案
        
        Args:
            style: 颜色方案风格，可选值: dark_mode, light_mode, modern, professional
        
        Returns:
            Dict: 颜色方案字典
        """
        if style in self.color_palettes:
            return self.color_palettes[style]
        else:
            # 生成随机颜色方案
            return self._generate_random_palette()
    
    def _generate_random_palette(self) -> Dict[str, str]:
        """
        生成随机颜色方案
        
        Returns:
            Dict: 随机颜色方案字典
        """
        # 生成随机主色
        primary = self._generate_random_color()
        
        # 生成辅助色
        secondary = self._generate_random_color(exclude=[primary])
        
        # 生成中性色
        background = "#000000" if random.random() > 0.5 else "#FFFFFF"
        text = "#FFFFFF" if background == "#000000" else "#000000"
        
        # 生成表面色和边框色
        if background == "#000000":
            surface = f"#{random.randint(10, 30):02x}{random.randint(10, 30):02x}{random.randint(10, 30):02x}"
            border = f"#{random.randint(50, 80):02x}{random.randint(50, 80):02x}{random.randint(50, 80):02x}"
        else:
            surface = f"#{random.randint(220, 240):02x}{random.randint(220, 240):02x}{random.randint(220, 240):02x}"
            border = f"#{random.randint(180, 200):02x}{random.randint(180, 200):02x}{random.randint(180, 200):02x}"
        
        return {
            "primary": primary,
            "secondary": secondary,
            "background": background,
            "surface": surface,
            "text": text,
            "border": border
        }
    
    def _generate_random_color(self, exclude: List[str] = None) -> str:
        """
        生成随机颜色
        
        Args:
            exclude: 要排除的颜色列表
        
        Returns:
            str: 十六进制颜色代码
        """
        if exclude is None:
            exclude = []
        
        while True:
            # 生成随机RGB值
            r = random.randint(0, 255)
            g = random.randint(0, 255)
            b = random.randint(0, 255)
            
            # 转换为十六进制
            color = f"#{r:02x}{g:02x}{b:02x}"
            
            # 检查是否在排除列表中
            if color not in exclude:
                return color
    
    def generate_professional_report_css(self) -> str:
        """
        生成专业报告风格的CSS样式
        
        Returns:
            str: 专业报告风格的CSS样式代码
        """
        palette = self.color_palettes['professional_report']
        
        css = f"""
<style>
    /* ==================== 全局样式 ==================== */
    @import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');
    
    * {{
        margin: 0;
        padding: 0;
        box-sizing: border-box;
    }}
    
    html, body, .stApp {{
        background: linear-gradient(135deg, {palette['background']} 0%, #E0F2FE 100%) !important;
        color: {palette['text']} !important;
        font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif !important;
    }}
    
    /* ==================== 侧边栏 ==================== */
    [data-testid="stSidebar"] {{
        background: linear-gradient(180deg, {palette['surface']} 0%, {palette['surface_dark']} 100%) !important;
        border-right: 1px solid {palette['border']} !important;
        box-shadow: 2px 0 8px rgba(0, 0, 0, 0.05) !important;
    }}
    
    [data-testid="stSidebar"] > div:first-child {{
        padding-top: 2rem !important;
    }}
    
    /* 侧边栏标题 */
    [data-testid="stSidebar"] h1 {{
        color: {palette['primary']} !important;
        font-size: 1.5rem !important;
        font-weight: 700 !important;
        padding: 0.5rem 1rem !important;
        border-bottom: 2px solid {palette['primary']} !important;
        margin-bottom: 1.5rem !important;
    }}
    
    [data-testid="stSidebar"] h2 {{
        color: {palette['text']} !important;
        font-size: 0.95rem !important;
        font-weight: 600 !important;
        text-transform: uppercase !important;
        letter-spacing: 0.05em !important;
        padding: 0.75rem 1rem 0.5rem 1rem !important;
        margin-top: 1.5rem !important;
        border-left: 3px solid {palette['secondary']} !important;
        background: {palette['border_light']} !important;
    }}
    
    /* ==================== 主标题样式 ==================== */
    .title {{
        font-size: 2.5rem !important;
        font-weight: 700 !important;
        background: linear-gradient(135deg, {palette['primary']} 0%, {palette['info']} 100%) !important;
        -webkit-background-clip: text !important;
        -webkit-text-fill-color: transparent !important;
        background-clip: text !important;
        margin-bottom: 0.5rem !important;
        text-align: center !important;
        letter-spacing: -0.02em !important;
    }}
    
    .subtitle {{
        font-size: 1.1rem !important;
        color: {palette['text_light']} !important;
        text-align: center !important;
        margin-bottom: 2rem !important;
        font-weight: 400 !important;
    }}
    
    /* ==================== 标题层级 ==================== */
    h1, h2, h3 {{
        color: {palette['text']} !important;
        font-weight: 600 !important;
    }}
    
    h1 {{
        font-size: 2rem !important;
        border-bottom: 3px solid {palette['primary']} !important;
        padding-bottom: 0.5rem !important;
        margin-bottom: 1.5rem !important;
    }}
    
    h2 {{
        font-size: 1.5rem !important;
        margin-top: 2rem !important;
        margin-bottom: 1rem !important;
        padding-left: 0.75rem !important;
        border-left: 4px solid {palette['secondary']} !important;
    }}
    
    h3 {{
        font-size: 1.25rem !important;
        margin-top: 1.5rem !important;
        margin-bottom: 0.75rem !important;
        color: {palette['primary']} !important;
    }}
    
    /* ==================== 按钮样式 ==================== */
    .stButton > button {{
        background: linear-gradient(135deg, {palette['primary']} 0%, #1E3A8A 100%) !important;
        color: white !important;
        border: none !important;
        border-radius: 8px !important;
        padding: 0.75rem 1.5rem !important;
        font-weight: 600 !important;
        font-size: 0.95rem !important;
        transition: all 0.3s ease !important;
        box-shadow: 0 2px 8px rgba(30, 64, 175, 0.3) !important;
        text-transform: uppercase !important;
        letter-spacing: 0.05em !important;
    }}
    
    .stButton > button:hover {{
        transform: translateY(-2px) !important;
        box-shadow: 0 4px 12px rgba(30, 64, 175, 0.4) !important;
        background: linear-gradient(135deg, #1E3A8A 0%, {palette['primary']} 100%) !important;
    }}
    
    .stButton > button:active {{
        transform: translateY(0) !important;
    }}
    
    /* 下载按钮特殊样式 */
    .stDownloadButton > button {{
        background: linear-gradient(135deg, {palette['success']} 0%, #059669 100%) !important;
        box-shadow: 0 2px 8px rgba(16, 185, 129, 0.3) !important;
    }}
    
    .stDownloadButton > button:hover {{
        box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4) !important;
    }}
    
    /* ==================== 输入框样式 ==================== */
    .stTextInput > div > div > input,
    .stNumberInput > div > div > input,
    .stSelectbox > div > div > div,
    .stTextArea > div > div > textarea {{
        background-color: {palette['surface']} !important;
        color: {palette['text']} !important;
        border: 2px solid {palette['border']} !important;
        border-radius: 8px !important;
        padding: 0.75rem !important;
        font-size: 0.95rem !important;
transition: all 0.3s ease !important;
    }}
    
    .stTextInput > div > div > input:focus,
    .stNumberInput > div > div > input:focus,
    .stSelectbox > div > div > div:focus,
    .stTextArea > div > div > textarea:focus {{
        border-color: {palette['primary']} !important;
        outline: none !important;
        box-shadow: 0 0 0 3px rgba(30, 64, 175, 0.1) !important;
    }}
    
    /* Slider样式 */
    .stSlider > div > div > div > div {{
        background-color: {palette['primary']} !important;
    }}
    
    /* ==================== 数据表格样式 ==================== */
    .stDataFrame {{
        border-radius: 12px !important;
        overflow: hidden !important;
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08) !important;
    }}
    
    .dataframe {{
        border: none !important;
        border-radius: 12px !important;
        overflow: hidden !important;
    }}
    
    .dataframe thead tr {{
        background: linear-gradient(135deg, {palette['primary']} 0%, #1E3A8A 100%) !important;
    }}
    
    .dataframe thead th {{
        color: white !important;
        font-weight: 600 !important;
        font-size: 0.9rem !important;
        text-transform: uppercase !important;
        letter-spacing: 0.05em !important;
        padding: 1rem !important;
        border: none !important;
    }}
    
    .dataframe tbody tr {{
        background-color: {palette['surface']} !important;
        transition: all 0.2s ease !important;
    }}
    
    .dataframe tbody tr:nth-child(even) {{
        background-color: {palette['surface_dark']} !important;
    }}
    
    .dataframe tbody tr:hover {{
        background-color: {palette['border_light']} !important;
        transform: scale(1.01) !important;
    }}
    
    .dataframe tbody td {{
        color: {palette['text']} !important;
        padding: 0.875rem !important;
        border-bottom: 1px solid {palette['border_light']} !important;
        font-size: 0.9rem !important;
    }}
    
    /* ==================== Metric组件样式 ==================== */
    [data-testid="stMetric"] {{
        background: {palette['surface']} !important;
        padding: 1.5rem !important;
        border-radius: 12px !important;
        border: 1px solid {palette['border']} !important;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05) !important;
        transition: all 0.3s ease !important;
    }}
    
    [data-testid="stMetric"]:hover {{
        transform: translateY(-4px) !important;
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12) !important;
    }}
    
    [data-testid="stMetricLabel"] {{
        color: {palette['text_light']} !important;
        font-size: 0.85rem !important;
        font-weight: 600 !important;
        text-transform: uppercase !important;
        letter-spacing: 0.05em !important;
    }}
    
    [data-testid="stMetricValue"] {{
        color: {palette['primary']} !important;
        font-size: 2rem !important;
        font-weight: 700 !important;
    }}
    
    [data-testid="stMetricDelta"] {{
        font-size: 0.9rem !important;
        font-weight: 600 !important;
    }}
    
    /* ==================== 消息框样式 ==================== */
    .stAlert {{
        border-radius: 12px !important;
        border: none !important;
        padding: 1rem 1.5rem !important;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08) !important;
    }}
    
    [data-testid="stSuccess"] {{
        background: linear-gradient(135deg, {palette['success']} 0%, #059669 100%) !important;
        color: white !important;
    }}
    
    [data-testid="stWarning"] {{
        background: linear-gradient(135deg, {palette['warning']} 0%, #D97706 100%) !important;
        color: white !important;
    }}
    
    [data-testid="stError"] {{
        background: linear-gradient(135deg, {palette['danger']} 0%, #B91C1C 100%) !important;
        color: white !important;
    }}
    
    [data-testid="stInfo"] {{
        background: linear-gradient(135deg, {palette['info']} 0%, #2563EB 100%) !important;
        color: white !important;
    }}
    
    /* ==================== 卡片样式 ==================== */
    .card {{
        background: {palette['surface']} !important;
        border: 1px solid {palette['border']} !important;
        border-radius: 16px !important;
        padding: 2rem !important;
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08) !important;
        margin-bottom: 1.5rem !important;
        transition: all 0.3s ease !important;
    }}
    
    .card:hover {{
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12) !important;
        transform: translateY(-2px) !important;
    }}
    
    /* ==================== 图表容器 ==================== */
    .stPlotlyChart {{
        background: {palette['surface']} !important;
        border-radius: 16px !important;
        padding: 1.5rem !important;
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08) !important;
        margin: 1.5rem 0 !important;
    }}
    
    /* ==================== Expander样式 ==================== */
    .streamlit-expanderHeader {{
        background: {palette['surface']} !important;
        border: 1px solid {palette['border']} !important;
        border-radius: 12px !important;
        padding: 1rem 1.5rem !important;
        font-weight: 600 !important;
        color: {palette['text']} !important;
        transition: all 0.3s ease !important;
    }}
    
    .streamlit-expanderHeader:hover {{
        background: {palette['surface_dark']} !important;
        border-color: {palette['primary']} !important;
    }}
    
    .streamlit-expanderContent {{
        background: {palette['surface']} !important;
        border: 1px solid {palette['border']} !important;
        border-top: none !important;
        border-radius: 0 0 12px 12px !important;
        padding: 1.5rem !important;
    }}
    
    /* ==================== 分隔线 ==================== */
    hr {{
        border: none !important;
        height: 2px !important;
        background: linear-gradient(90deg, transparent 0%, {palette['border']} 50%, transparent 100%) !important;
        margin: 2rem 0 !important;
    }}
    
    /* ==================== Spinner样式 ==================== */
    .stSpinner > div {{
        border-top-color: {palette['primary']} !important;
    }}
    
    /* ==================== 复选框和单选框 ==================== */
    .stCheckbox > label {{
        color: {palette['text']} !important;
        font-weight: 500 !important;
    }}
    
    .stRadio > label {{
        color: {palette['text']} !important;
        font-weight: 500 !important;
    }}
    
    /* ==================== 标签页 ==================== */
    .stTabs [data-baseweb="tab-list"] {{
        gap: 1rem !important;
        background-color: transparent !important;
    }}
    
    .stTabs [data-baseweb="tab"] {{
        background-color: {palette['surface']} !important;
        border-radius: 8px 8px 0 0 !important;
        padding: 0.75rem 1.5rem !important;
        color: {palette['text_light']} !important;
        font-weight: 600 !important;
        border: 2px solid {palette['border']} !important;
        border-bottom: none !important;
    }}
    
    .stTabs [aria-selected="true"] {{
        background: linear-gradient(135deg, {palette['primary']} 0%, #1E3A8A 100%) !important;
        color: white !important;
        border-color: {palette['primary']} !important;
    }}
    
    /* ==================== 页脚样式 ==================== */
    .footer {{
        text-align: center !important;
        padding: 2rem !important;
        margin-top: 3rem !important;
        border-top: 2px solid {palette['border']} !important;
        color: {palette['text_light']} !important;
        font-size: 0.9rem !important;
    }}
    
    /* ==================== 响应式设计 ==================== */
    @media (max-width: 768px) {{
        .title {{
            font-size: 1.75rem !important;
        }}
        
        .subtitle {{
            font-size: 1rem !important;
        }}
        
        [data-testid="stMetricValue"] {{
            font-size: 1.5rem !important;
        }}
        
        .card {{
            padding: 1.25rem !important;
        }}
    }}
    
    /* ==================== 滚动条美化 ==================== */
    ::-webkit-scrollbar {{
        width: 10px !important;
        height: 10px !important;
    }}
    
    ::-webkit-scrollbar-track {{
        background: {palette['surface_dark']} !important;
        border-radius: 5px !important;
    }}
    
    ::-webkit-scrollbar-thumb {{
        background: linear-gradient(135deg, {palette['primary']} 0%, {palette['secondary']} 100%) !important;
        border-radius: 5px !important;
    }}
    
    ::-webkit-scrollbar-thumb:hover {{
        background: linear-gradient(135deg, {palette['secondary']} 0%, {palette['primary']} 100%) !important;
    }}
    
    /* ==================== 动画效果 ==================== */
    @keyframes fadeIn {{
        from {{
            opacity: 0;
            transform: translateY(20px);
        }}
        to {{
            opacity: 1;
            transform: translateY(0);
        }}
    }}
    
    .stApp > div {{
        animation: fadeIn 0.5s ease-out !important;
    }}
    
    /* ==================== 链接样式 ==================== */
    a {{
        color: {palette['primary']} !important;
        text-decoration: none !important;
        font-weight: 600 !important;
        transition: all 0.3s ease !important;
    }}
    
    a:hover {{
        color: {palette['secondary']} !important;
        text-decoration: underline !important;
    }}
</style>
"""
        return css
    
    def generate_css_style(self, palette: Dict[str, str]) -> str:
        """
        根据颜色方案生成CSS样式
        
        Args:
            palette: 颜色方案字典
        
        Returns:
            str: CSS样式代码
        """
        # 直接构建CSS字符串，避免字符串格式化冲突
        css = """
<style>
    /* 全局样式 */
    * {
        margin: 0;
        padding: 0;
        box-sizing: border-box;
    }
    
    /* 背景和文字颜色 */
    html, body, .stApp {
        background-color: """
        css += palette['background']
        css += """ !important;
        color: """
        css += palette['text']
        css += """ !important;
    }
    
    /* 侧边栏 */
    [data-testid="stSidebar"] {
        background-color: """
        css += palette['surface']
        css += """ !important;
    }
    
    /* 标题样式 */
    .title {
        font-size: 32px !important;
        font-weight: bold !important;
        color: """
        css += palette['primary']
        css += """ !important;
    }
    
    .subtitle {
        font-size: 18px !important;
        color: """
        css += palette['text']
        css += """ !important;
    }
    
    /* 按钮样式 */
    button {
        color: """
        css += palette['text']
        css += """ !important;
        background-color: """
        css += palette['surface']
        css += """ !important;
        border: 1px solid """
        css += palette['border']
        css += """ !important;
        border-radius: 4px !important;
        padding: 8px 16px !important;
        transition: all 0.3s ease !important;
    }
    
    button:hover {
        background-color: """
        css += palette['primary']
        css += """ !important;
        color: """
        css += palette['background']
        css += """ !important;
        border-color: """
        css += palette['primary']
        css += """ !important;
    }
    
    /* 输入框样式 */
    input, textarea, select {
        color: """
        css += palette['text']
        css += """ !important;
        background-color: """
        css += palette['surface']
        css += """ !important;
        border: 1px solid """
        css += palette['border']
        css += """ !important;
        border-radius: 4px !important;
        padding: 8px !important;
    }
    
    input:focus, textarea:focus, select:focus {
        border-color: """
        css += palette['primary']
        css += """ !important;
        outline: none !important;
        box-shadow: 0 0 0 2px rgba("""
        css += self._hex_to_rgb(palette['primary'])
        css += """, 0.2) !important;
    }
    
    /* 表格样式 */
    table, th, td {
        color: """
        css += palette['text']
        css += """ !important;
        background-color: """
        css += palette['surface']
        css += """ !important;
        border: 1px solid """
        css += palette['border']
        css += """ !important;
        border-collapse: collapse !important;
    }
    
    th {
        background-color: """
        css += palette['surface']
        css += """ !important;
        font-weight: bold !important;
        padding: 8px !important;
    }
    
    td {
        padding: 8px !important;
    }
    
    /* Metric组件 */
    [data-testid="stMetricValue"] {
        color: """
        css += palette['secondary']
        css += """ !important;
    }
    
    [data-testid="stMetricLabel"] {
        color: """
        css += palette['text']
        css += """ !important;
    }
    
    /* 消息框 */
    [data-testid="stError"] {
        background-color: rgba(255, 0, 0, 0.1) !important;
        color: """
        css += palette['text']
        css += """ !important;
        border: 1px solid rgba(255, 0, 0, 0.3) !important;
    }
    
    [data-testid="stWarning"] {
        background-color: rgba(255, 165, 0, 0.1) !important;
        color: """
        css += palette['text']
        css += """ !important;
        border: 1px solid rgba(255, 165, 0, 0.3) !important;
    }
    
    [data-testid="stSuccess"] {
        background-color: rgba(0, 255, 0, 0.1) !important;
        color: """
        css += palette['text']
        css += """ !important;
        border: 1px solid rgba(0, 255, 0, 0.3) !important;
    }
    
    [data-testid="stInfo"] {
        background-color: rgba(0, 0, 255, 0.1) !important;
        color: """
        css += palette['text']
        css += """ !important;
        border: 1px solid rgba(0, 0, 255, 0.3) !important;
    }
    
    /* 卡片样式 */
    .card {
        background-color: """
        css += palette['surface']
        css += """ !important;
        border: 1px solid """
        css += palette['border']
        css += """ !important;
        border-radius: 8px !important;
        padding: 16px !important;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1) !important;
        margin-bottom: 16px !important;
    }
    
    /* 响应式设计 */
    @media (max-width: 768px) {
        .title {
            font-size: 24px !important;
        }
        
        .subtitle {
            font-size: 16px !important;
        }
    }
</style>
"""
        return css
    
    def _hex_to_rgb(self, hex_color: str) -> str:
        """
        将十六进制颜色转换为RGB
        
        Args:
            hex_color: 十六进制颜色代码
        
        Returns:
            str: RGB颜色值，格式为 "r, g, b"
        """
        # 移除 # 号
        hex_color = hex_color.lstrip('#')
        
        # 转换为RGB
        r = int(hex_color[0:2], 16)
        g = int(hex_color[2:4], 16)
        b = int(hex_color[4:6], 16)
        
        return f"{r}, {g}, {b}"
    
    def generate_web_design_tips(self) -> List[str]:
        """
        生成网页设计提示
        
        Returns:
            List[str]: 网页设计提示列表
        """
        tips = [
            "保持简洁：不要在页面上放置过多元素，保持视觉整洁",
            "一致的颜色方案：选择2-3个主色调，并在整个网站中保持一致",
            "响应式设计：确保网站在不同设备上都能正常显示",
            "良好的排版：使用清晰的字体层次结构，确保文本易读",
            "适当的空白：使用空白空间来分隔内容，提高可读性",
            "直观的导航：确保用户能够轻松找到他们需要的信息",
            "加载速度：优化图片和代码，确保网站加载迅速",
            "可访问性：确保网站对所有用户（包括残障人士）都可访问",
            "视觉层次：使用大小、颜色和位置来创建视觉层次结构",
            "一致性：保持按钮、表单和其他元素的设计一致"
        ]
        
        # 随机返回5个提示
        return random.sample(tips, 5)


def get_web_designer() -> WebDesigner:
    """
    获取网页设计师实例
    
    Returns:
        WebDesigner: 网页设计师实例
    """
    return WebDesigner()

# 测试
if __name__ == '__main__':
    designer = WebDesigner()
    
    # 测试生成颜色方案
    print("测试生成颜色方案:")
    palette = designer.generate_color_palette("dark_mode")
    print(palette)
    
    # 测试生成CSS样式
    print("\n测试生成CSS样式:")
    css = designer.generate_css_style(palette)
    print(css[:500] + "...")
    
    # 测试生成网页设计提示
    print("\n测试生成网页设计提示:")
    tips = designer.generate_web_design_tips()
    for tip in tips:
        print(f"- {tip}")

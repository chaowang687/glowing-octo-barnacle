# -*- coding: utf-8 -*-
"""
财经新闻爬虫配置文件
包含API接口URL、请求头、日志配置等
"""

import os

# ==================== 项目路径配置 ====================
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
DATA_DIR = os.path.join(BASE_DIR, 'data')
DB_PATH = os.path.join(DATA_DIR, 'market_data.db')

# 确保数据目录存在
os.makedirs(DATA_DIR, exist_ok=True)

# ==================== API接口配置 ====================
# 东方财富API（相对稳定）
URLS = {
    # 新闻搜索接口
    "news_search": "https://search-api-web.eastmoney.com/search/jsonp",
    
    # 主力资金流向接口
    "fund_flow": "https://push2.eastmoney.com/api/qt/stock/fflow/kline/get",
    
    # 行业板块资金流向
    "sector_fund_flow": "https://push2.eastmoney.com/api/qt/clist/get",
    
    # 个股实时行情
    "realtime_quote": "https://push2ex.eastmoney.com/getTopicZDFenBu",
    
    # 涨停板分布
    "limit_up": "https://push2ex.eastmoney.com/getTopicZTPool",
    
    # 龙虎榜数据
    "dragon_tiger": "https://push2ex.eastmoney.com/getTopicLHBPool",
    
    # 大单交易数据
    "large_trade": "https://push2ex.eastmoney.com/getTopicDPPool",
}

# ==================== 请求头配置 ====================
HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
    "Accept-Language": "zh-CN,zh;q=0.9,en;q=0.8",
    "Accept-Encoding": "gzip, deflate, br",
    "Connection": "keep-alive",
    "Referer": "https://www.eastmoney.com/",
}

# ==================== 爬虫配置 ====================
CRAWLER_CONFIG = {
    # 请求间隔（秒）
    "request_delay": (1.0, 2.5),
    
    # 最大重试次数
    "max_retries": 3,
    
    # 请求超时（秒）
    "timeout": 15,
    
    # 每页获取数量
    "page_size": 50,
}

# ==================== 日志配置 ====================
LOG_CONFIG = {
    "level": "INFO",
    "format": "<green>{time:YYYY-MM-DD HH:mm:ss}</green> | <level>{level: <8}</level> | <cyan>{name}</cyan>:<cyan>{function}</cyan>:<cyan>{line}</cyan> - <level>{message}</level>",
    "rotation": "10 MB",
    "retention": "7 days",
    "encoding": "utf-8",
}

# ==================== 数据库表配置 ====================
DB_TABLES = {
    "news": """
        CREATE TABLE IF NOT EXISTS news (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            stock_code TEXT,
            stock_name TEXT,
            title TEXT,
            content TEXT,
            pub_date TEXT,
            source TEXT,
            url TEXT UNIQUE,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """,
    "funds": """
        CREATE TABLE IF NOT EXISTS funds (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            stock_code TEXT,
            stock_name TEXT,
            trade_date TEXT,
            main_net_inflow REAL,
            main_net_inflow_ratio REAL,
            retail_net_inflow REAL,
            super_net_inflow REAL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            UNIQUE(stock_code, trade_date)
        )
    """,
    "sectors": """
        CREATE TABLE IF NOT EXISTS sectors (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            sector_name TEXT,
            trade_date TEXT,
            net_inflow REAL,
            change_percent REAL,
            turnover_rate REAL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            UNIQUE(sector_name, trade_date)
        )
    """,
    "events": """
        CREATE TABLE IF NOT EXISTS events (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            stock_code TEXT,
            stock_name TEXT,
            event_type TEXT,
            event_date TEXT,
            event_title TEXT,
            event_content TEXT,
            source TEXT,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    """,
}

# ==================== 股票代码映射 ====================
# 简单映射：判断市场类型
def get_market_type(stock_code: str) -> str:
    """
    根据股票代码判断市场类型
    上海: 6开头 -> 1
    深圳: 0,3开头 -> 0
    创业板: 3开头 -> 0
    科创板: 688开头 -> 1
    """
    if not stock_code:
        return "1"  # 默认上海
    
    if stock_code.startswith("6") or stock_code.startswith("688"):
        return "1"  # 上海
    else:
        return "0"  # 深圳

def format_stock_code(stock_code: str) -> str:
    """
    格式化股票代码为市场代码格式
    600519 -> 1.600519
    000001 -> 0.000001
    """
    market_type = get_market_type(stock_code)
    return f"{market_type}.{stock_code}"

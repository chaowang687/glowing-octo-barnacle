#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
配置管理模块
支持配置文件和环境变量
"""

import os
import configparser
from typing import Dict, Optional


class ConfigManager:
    """
    配置管理器
    支持从配置文件和环境变量加载配置
    """
    
    def __init__(self, config_file: str = 'config.ini'):
        """
        初始化配置管理器
        
        Args:
            config_file: 配置文件路径
        """
        self.config_file = config_file
        self.config = configparser.ConfigParser()
        
        # 加载配置文件
        if os.path.exists(config_file):
            self.config.read(config_file)
        
        # 配置默认值
        self._set_defaults()
    
    def _set_defaults(self):
        """
        设置默认配置
        """
        # 网络配置
        if not self.config.has_section('network'):
            self.config.add_section('network')
        
        # 代理配置
        if not self.config.has_option('network', 'proxy_enabled'):
            self.config.set('network', 'proxy_enabled', 'false')
        
        if not self.config.has_option('network', 'http_proxy'):
            self.config.set('network', 'http_proxy', '')
        
        if not self.config.has_option('network', 'https_proxy'):
            self.config.set('network', 'https_proxy', '')
        
        # 数据配置
        if not self.config.has_section('data'):
            self.config.add_section('data')
        
        if not self.config.has_option('data', 'timeout'):
            self.config.set('data', 'timeout', '30')
        
        if not self.config.has_option('data', 'retry_times'):
            self.config.set('data', 'retry_times', '3')
        
        if not self.config.has_option('data', 'cache_enabled'):
            self.config.set('data', 'cache_enabled', 'true')
        
        if not self.config.has_option('data', 'cache_dir'):
            self.config.set('data', 'cache_dir', './cache')
        
        # 缠论配置
        if not self.config.has_section('chanlun'):
            self.config.add_section('chanlun')
        
        if not self.config.has_option('chanlun', 'bi_threshold'):
            self.config.set('chanlun', 'bi_threshold', '0.03')
        
        if not self.config.has_option('chanlun', 'use_macd'):
            self.config.set('chanlun', 'use_macd', 'true')
    
    def get(self, section: str, option: str, default: str = '') -> str:
        """
        获取配置值
        优先从环境变量获取，其次从配置文件获取
        
        Args:
            section: 配置 section
            option: 配置项
            default: 默认值
        
        Returns:
            配置值
        """
        # 尝试从环境变量获取
        env_key = f"{section.upper()}_{option.upper()}"
        if env_key in os.environ:
            return os.environ[env_key]
        
        # 从配置文件获取
        try:
            return self.config.get(section, option)
        except (configparser.NoSectionError, configparser.NoOptionError):
            return default
    
    def get_bool(self, section: str, option: str, default: bool = False) -> bool:
        """
        获取布尔类型配置
        
        Args:
            section: 配置 section
            option: 配置项
            default: 默认值
        
        Returns:
            布尔值配置
        """
        value = self.get(section, option, str(default))
        return value.lower() in ('true', '1', 'yes', 'y', 't')
    
    def get_int(self, section: str, option: str, default: int = 0) -> int:
        """
        获取整数类型配置
        
        Args:
            section: 配置 section
            option: 配置项
            default: 默认值
        
        Returns:
            整数配置值
        """
        value = self.get(section, option, str(default))
        try:
            return int(value)
        except ValueError:
            return default
    
    def get_float(self, section: str, option: str, default: float = 0.0) -> float:
        """
        获取浮点数类型配置
        
        Args:
            section: 配置 section
            option: 配置项
            default: 默认值
        
        Returns:
            浮点数配置值
        """
        value = self.get(section, option, str(default))
        try:
            return float(value)
        except ValueError:
            return default
    
    def get_proxies(self) -> Dict[str, str]:
        """
        获取代理配置
        
        Returns:
            代理配置字典
        """
        proxies = {}
        
        if self.get_bool('network', 'proxy_enabled'):
            http_proxy = self.get('network', 'http_proxy')
            https_proxy = self.get('network', 'https_proxy')
            
            if http_proxy:
                proxies['http'] = http_proxy
            
            if https_proxy:
                proxies['https'] = https_proxy
        
        return proxies
    
    def save(self):
        """
        保存配置到文件
        """
        with open(self.config_file, 'w') as f:
            self.config.write(f)


# 全局配置实例
config_manager = ConfigManager()

# 添加DB_PATH变量，与FinNewsCrawler_v2/config.py保持兼容
import os
DB_PATH = os.path.join(os.path.dirname(__file__), 'FinNewsCrawler_v2', 'data', 'market_data.db')


# 便捷函数
def get_config() -> ConfigManager:
    """
    获取配置管理器实例
    
    Returns:
        配置管理器实例
    """
    return config_manager


def get_proxies() -> Dict[str, str]:
    """
    获取代理配置
    
    Returns:
        代理配置字典
    """
    return config_manager.get_proxies()


def get_data_config() -> Dict[str, any]:
    """
    获取数据配置
    
    Returns:
        数据配置字典
    """
    return {
        'timeout': config_manager.get_int('data', 'timeout'),
        'retry_times': config_manager.get_int('data', 'retry_times'),
        'cache_enabled': config_manager.get_bool('data', 'cache_enabled'),
        'cache_dir': config_manager.get('data', 'cache_dir'),
    }


def get_chanlun_config() -> Dict[str, any]:
    """
    获取缠论配置
    
    Returns:
        缠论配置字典
    """
    return {
        'bi_threshold': config_manager.get_float('chanlun', 'bi_threshold'),
        'use_macd': config_manager.get_bool('chanlun', 'use_macd'),
    }

# 添加与FinNewsCrawler_v2/config.py兼容的变量
import os

# 项目路径配置
BASE_DIR = os.path.dirname(__file__)
DATA_DIR = os.path.join(BASE_DIR, 'FinNewsCrawler_v2', 'data')
DB_PATH = os.path.join(DATA_DIR, 'market_data.db')

# 确保数据目录存在
os.makedirs(DATA_DIR, exist_ok=True)

# API接口配置
URLS = {
    "news_search": "https://search-api-web.eastmoney.com/search/jsonp",
    "fund_flow": "https://push2.eastmoney.com/api/qt/stock/fflow/kline/get",
    "sector_fund_flow": "https://push2.eastmoney.com/api/qt/clist/get",
    "realtime_quote": "https://push2ex.eastmoney.com/getTopicZDFenBu",
    "limit_up": "https://push2ex.eastmoney.com/getTopicZTPool",
    "dragon_tiger": "https://push2ex.eastmoney.com/getTopicLHBPool",
    "large_trade": "https://push2ex.eastmoney.com/getTopicDPPool",
}

# 请求头配置
HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
    "Accept-Language": "zh-CN,zh;q=0.9,en;q=0.8",
    "Accept-Encoding": "gzip, deflate, br",
    "Connection": "keep-alive",
    "Referer": "https://www.eastmoney.com/",
}

# 爬虫配置
CRAWLER_CONFIG = {
    "request_delay": (1.0, 2.5),
    "max_retries": 3,
    "timeout": 15,
    "page_size": 50,
}

# 日志配置
LOG_CONFIG = {
    "level": "INFO",
    "format": "<green>{time:YYYY-MM-DD HH:mm:ss}</green> | <level>{level: <8}</level> | <cyan>{name}</cyan>:<cyan>{function}</cyan>:<cyan>{line}</cyan> - <level>{message}</level>",
    "rotation": "10 MB",
    "retention": "7 days",
    "encoding": "utf-8",
}

# 数据库表配置
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

# 股票代码映射函数
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

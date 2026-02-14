# -*- coding: utf-8 -*-
"""
网络爬虫模块
负责从各数据源获取原始数据，包含请求调度、重试机制等
"""

import requests
import json
import re
import time
import random
from datetime import datetime, timedelta
from typing import Optional, Dict, List, Any

# 使用硬编码的方式导入模块
import sys
import os

import sys
import os
from quant_system import config
from quant_system.utils import logger
from .parser import NewsParser, FundParser, SectorParser

# 使用 config 中的变量
BASE_DIR = config.BASE_DIR
DATA_DIR = config.DATA_DIR
DB_PATH = config.DB_PATH

# 确保数据目录存在
os.makedirs(DATA_DIR, exist_ok=True)


class RetryableError(Exception):
    """可重试的错误"""
    pass


class PermanentError(Exception):
    """不可重试的错误"""
    pass


class FinancialCrawler:
    """财经数据爬虫基类"""
    
    def __init__(self):
        """初始化爬虫"""
        self.session = requests.Session()
        self.session.headers.update(config.HEADERS)
        self.request_delay = config.CRAWLER_CONFIG["request_delay"]
        self.max_retries = config.CRAWLER_CONFIG["max_retries"]
        self.timeout = config.CRAWLER_CONFIG["timeout"]
        
        # 初始化解析器
        self.news_parser = NewsParser()
        self.fund_parser = FundParser()
        self.sector_parser = SectorParser()
        
        logger.info("爬虫初始化完成")
    
    def _random_delay(self):
        """随机延迟，避免请求过快"""
        delay = random.uniform(*self.request_delay)
        time.sleep(delay)
    
    def _request_with_retry(self, url: str, params: Optional[Dict] = None, 
                            method: str = "GET", data: Optional[Dict] = None) -> requests.Response:
        """
        带重试机制的请求
        
        Args:
            url: 请求URL
            params: GET参数
            method: 请求方法
            data: POST数据
            
        Returns:
            Response对象
            
        Raises:
            PermanentError: 不可重试的错误
        """
        last_error = None
        
        for attempt in range(self.max_retries):
            try:
                self._random_delay()
                
                if method.upper() == "GET":
                    response = self.session.get(
                        url, 
                        params=params, 
                        timeout=self.timeout
                    )
                else:
                    response = self.session.post(
                        url, 
                        params=params, 
                        json=data,
                        timeout=self.timeout
                    )
                
                # 检查HTTP状态码
                if response.status_code == 200:
                    return response
                elif response.status_code == 429:
                    # 限流，增加等待时间
                    wait_time = random.uniform(10, 30)
                    logger.warning(f"触发限流，等待 {wait_time:.1f} 秒后重试")
                    time.sleep(wait_time)
                    last_error = f"HTTP 429: Rate limited"
                elif response.status_code == 404:
                    raise PermanentError(f"HTTP 404: 资源不存在 - {url}")
                elif response.status_code >= 500:
                    last_error = f"HTTP {response.status_code}: 服务器错误"
                else:
                    raise PermanentError(f"HTTP {response.status_code}: {response.text[:100]}")
                    
            except requests.exceptions.Timeout:
                last_error = "请求超时"
                logger.warning(f"请求超时 (尝试 {attempt + 1}/{self.max_retries})")
            except requests.exceptions.ConnectionError as e:
                last_error = f"连接错误: {e}"
                logger.warning(f"连接错误 (尝试 {attempt + 1}/{self.max_retries})")
            except requests.exceptions.RequestException as e:
                last_error = f"请求异常: {e}"
                logger.warning(f"请求异常 (尝试 {attempt + 1}/{self.max_retries})")
        
        raise RetryableError(f"重试 {self.max_retries} 次后仍失败: {last_error}")
    
    def close(self):
        """关闭会话"""
        self.session.close()


class NewsCrawler(FinancialCrawler):
    """新闻数据爬虫"""
    
    def __init__(self):
        super().__init__()
        self.base_url = config.URLS["news_search"]
    
    def search_news(self, keyword: str, days: int = 7, max_results: int = 50) -> List[Dict]:
        """
        搜索新闻
        
        Args:
            keyword: 搜索关键词（股票名称或代码）
            days: 查询天数
            max_results: 最大结果数
            
        Returns:
            新闻列表
        """
        logger.info(f"开始搜索新闻: keyword={keyword}, days={days}")
        
        # 计算截止日期
        end_date = datetime.now()
        start_date = end_date - timedelta(days=days)
        
        # 构建请求参数
        params = {
            "cb": f"jQuery{random.randint(100000, 999999)}",
            "param": json.dumps({
                "uid": "",
                "keyword": keyword,
                "type": ["cmsArticle"],
                "client": "web",
                "clientType": "web",
                "pageNum": 1,
                "pageSize": max_results,
                "sort": "date",
                "dateType": "true",
                "startDate": start_date.strftime("%Y-%m-%d"),
                "endDate": end_date.strftime("%Y-%m-%d"),
            }, ensure_ascii=False)
        }
        
        try:
            response = self._request_with_retry(self.base_url, params=params)
            response.encoding = 'utf-8'
            
            # 解析JSONP响应
            text = response.text
            json_str = re.search(r'\((.*)\)', text)
            if json_str:
                data = json.loads(json_str.group(1))
            else:
                data = {}
            
            # 解析新闻数据
            news_list = self.news_parser.parse_eastmoney_news(data, keyword)
            
            # 按日期过滤
            cutoff = start_date.strftime("%Y-%m-%d")
            news_list = [
                n for n in news_list 
                if n.get('pub_date', '').replace('-', '').replace(' ', '') >= cutoff.replace('-', '')
            ]
            
            logger.info(f"搜索完成，获取到 {len(news_list)} 条新闻")
            return news_list
            
        except Exception as e:
            logger.error(f"搜索新闻失败: {e}")
            return []
    
    def search_stock_news(self, stock_code: str, stock_name: str = "", 
                          days: int = 7) -> List[Dict]:
        """
        搜索指定股票的新闻
        
        Args:
            stock_code: 股票代码
            stock_name: 股票名称
            days: 查询天数
            
        Returns:
            新闻列表
        """
        # 优先使用名称搜索，名称为空则使用代码
        keyword = stock_name if stock_name else stock_code
        news_list = self.search_news(keyword, days)
        
        # 标记股票代码
        for news in news_list:
            if not news.get('stock_code'):
                news['stock_code'] = stock_code
            if not news.get('stock_name') and stock_name:
                news['stock_name'] = stock_name
        
        return news_list


class FundCrawler(FinancialCrawler):
    """资金流向数据爬虫"""
    
    def __init__(self):
        super().__init__()
        self.base_url = config.URLS["fund_flow"]
    
    def get_fund_flow(self, stock_code: str, days: int = 30) -> List[Dict]:
        """
        获取股票资金流向
        
        Args:
            stock_code: 股票代码
            days: 查询天数
            
        Returns:
            资金流向列表
        """
        logger.info(f"获取资金流向: {stock_code}, days={days}")
        
        # 格式化股票代码
        secid = config.format_stock_code(stock_code)
        
        params = {
            "lmt": days,
            "klt": 101,  # 日K线
            "secid": secid,
            "fields1": "f1,f2,f3,f7",
            "fields2": "f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61,f62,f63",
        }
        
        try:
            response = self._request_with_retry(self.base_url, params=params)
            data = response.json()
            
            # 解析数据
            funds_list = self.fund_parser.parse_eastmoney_funds(data, stock_code)
            
            logger.info(f"获取资金流向完成: {len(funds_list)} 条")
            return funds_list
            
        except Exception as e:
            logger.error(f"获取资金流向失败: {e}")
            return []


class SectorCrawler(FinancialCrawler):
    """行业板块数据爬虫"""
    
    def __init__(self):
        super().__init__()
        self.base_url = config.URLS["sector_fund_flow"]
    
    def get_sector_fund_flow(self, trade_date: str = "") -> List[Dict]:
        """
        获取行业板块资金流向
        
        Args:
            trade_date: 交易日期，默认今天
            
        Returns:
            行业板块列表
        """
        if not trade_date:
            trade_date = datetime.now().strftime("%Y-%m-%d")
        
        logger.info(f"获取行业板块资金流向: {trade_date}")
        
        params = {
            "pn": 1,
            "pz": 200,
            "po": 1,
            "np": 1,
            "fltt": 2,
            "invt": 2,
            "fid": "f3",  # 按涨幅排序
            "fs": "m:90+t:2",  # 行业板块
            "fields": "f1,f2,f3,f4,f12,f13,f14",
        }
        
        try:
            response = self._request_with_retry(self.base_url, params=params)
            data = response.json()
            
            # 解析数据
            sectors_list = self.sector_parser.parse_eastmoney_sectors(data, trade_date)
            
            logger.info(f"获取行业板块完成: {len(sectors_list)} 个")
            return sectors_list
            
        except Exception as e:
            logger.error(f"获取行业板块失败: {e}")
            return []
    
    def get_top_sectors(self, trade_date: str = "", top_n: int = 10) -> Dict:
        """
        获取热门行业板块
        
        Args:
            trade_date: 交易日期
            top_n: 返回数量
            
        Returns:
            包含热门和冷门板块的字典
        """
        sectors = self.get_sector_fund_flow(trade_date)
        
        if not sectors:
            return {"hot": [], "cold": []}
        
        # 按净流入排序
        sectors.sort(key=lambda x: x['net_inflow'], reverse=True)
        
        return {
            "hot": sectors[:top_n],
            "cold": sectors[-top_n:][::-1]
        }


class RealtimeCrawler(FinancialCrawler):
    """实时行情数据爬虫"""
    
    def __init__(self):
        super().__init__()
    
    def get_realtime_quotes(self, stock_codes: List[str]) -> List[Dict]:
        """
        获取实时行情
        
        Args:
            stock_codes: 股票代码列表
            
        Returns:
            行情列表
        """
        if not stock_codes:
            return []
        
        # 格式化股票代码
        secids = [config.format_stock_code(code) for code in stock_codes]
        
        url = "https://push2.eastmoney.com/api/qt/ulist/get"
        params = {
            "fltt": 2,
            "invt": 2,
            "fields": "f2,f3,f4,f12,f13,f14",
            "secids": ",".join(secids),
        }
        
        try:
            response = self._request_with_retry(url, params=params)
            data = response.json()
            
            quotes = []
            for item in data.get('data', {}).get('diff', []):
                quotes.append({
                    'stock_code': item.get('f12'),
                    'stock_name': item.get('f14'),
                    'price': item.get('f2'),
                    'change_percent': item.get('f3'),
                })
            
            return quotes
            
        except Exception as e:
            logger.error(f"获取实时行情失败: {e}")
            return []


# ==================== 便捷函数 ====================

def crawl_stock_full_data(stock_code: str, stock_name: str = "", 
                          days: int = 7) -> Dict[str, Any]:
    """
    爬取单只股票的完整数据（新闻+资金流向+事件）
    
    Args:
        stock_code: 股票代码
        stock_name: 股票名称
        days: 查询天数
        
    Returns:
        包含各类数据的字典
    """
    logger.info(f"开始爬取 {stock_code} 的完整数据")
    
    news_crawler = NewsCrawler()
    fund_crawler = FundCrawler()
    
    result = {
        'stock_code': stock_code,
        'stock_name': stock_name,
        'news': [],
        'funds': [],
        'events': [],
    }
    
    # 获取新闻
    try:
        result['news'] = news_crawler.search_stock_news(stock_code, stock_name, days)
        
        # 从新闻中提取事件
        from modules.parser import EventParser
        event_parser = EventParser()
        result['events'] = event_parser.parse_events(result['news'])
    except Exception as e:
        logger.error(f"获取新闻失败: {e}")
    
    # 获取资金流向
    try:
        result['funds'] = fund_crawler.get_fund_flow(stock_code, days)
    except Exception as e:
        logger.error(f"获取资金流向失败: {e}")
    
    logger.info(f"数据爬取完成: 新闻{len(result['news'])}条, 资金{len(result['funds'])}条, 事件{len(result['events'])}条")
    
    # 关闭爬虫
    news_crawler.close()
    fund_crawler.close()
    
    return result


def crawl_market_overview(days: int = 7) -> Dict[str, Any]:
    """
    爬取市场概览数据（行业板块+主力资金）
    
    Args:
        days: 查询天数
        
    Returns:
        包含市场概览的字典
    """
    logger.info("开始爬取市场概览数据")
    
    sector_crawler = SectorCrawler()
    fund_crawler = FundCrawler()
    
    result = {
        'hot_sectors': [],
        'cold_sectors': [],
        'major_funds': [],
    }
    
    # 获取行业板块
    try:
        sectors = sector_crawler.get_top_sectors(top_n=10)
        result['hot_sectors'] = sectors.get('hot', [])
        result['cold_sectors'] = sectors.get('cold', [])
    except Exception as e:
        logger.error(f"获取行业板块失败: {e}")
    
    # 获取大盘资金流向（简单模拟：获取沪深300成分股）
    # 实际可以获取上证指数、深证成指的资金流向
    try:
        # 这里可以添加大盘资金流向的获取逻辑
        pass
    except Exception as e:
        logger.error(f"获取主力资金失败: {e}")
    
    sector_crawler.close()
    fund_crawler.close()
    
    logger.info("市场概览数据爬取完成")
    return result

# -*- coding: utf-8 -*-
"""
异步网络爬虫模块
基于 aiohttp 实现的高性能异步爬虫
"""

import asyncio
import aiohttp
import json
import re
import time
import random
from datetime import datetime, timedelta
from typing import Optional, Dict, List, Any

from quant_system import config
from quant_system.utils import logger
from .parser import NewsParser, FundParser, SectorParser

class AsyncFinancialCrawler:
    """异步财经数据爬虫基类"""
    
    def __init__(self):
        """初始化爬虫"""
        self.headers = config.HEADERS
        self.request_delay = config.CRAWLER_CONFIG["request_delay"]
        self.max_retries = config.CRAWLER_CONFIG["max_retries"]
        self.timeout = config.CRAWLER_CONFIG["timeout"]
        
        # 初始化解析器
        self.news_parser = NewsParser()
        self.fund_parser = FundParser()
        self.sector_parser = SectorParser()
        
    async def _random_delay(self):
        """随机延迟"""
        delay = random.uniform(*self.request_delay)
        await asyncio.sleep(delay)
    
    async def _request_with_retry(self, url: str, params: Optional[Dict] = None, 
                                method: str = "GET", data: Optional[Dict] = None) -> Any:
        """
        带重试机制的异步请求
        """
        async with aiohttp.ClientSession(headers=self.headers) as session:
            for attempt in range(self.max_retries):
                try:
                    await self._random_delay()
                    
                    timeout = aiohttp.ClientTimeout(total=self.timeout)
                    
                    if method.upper() == "GET":
                        async with session.get(url, params=params, timeout=timeout) as response:
                            if response.status == 200:
                                return await response.text()
                            elif response.status == 429:
                                wait_time = random.uniform(10, 30)
                                logger.warning(f"触发限流，等待 {wait_time:.1f} 秒后重试")
                                await asyncio.sleep(wait_time)
                            else:
                                logger.warning(f"HTTP {response.status}: {url}")
                                
                    else:
                        async with session.post(url, params=params, json=data, timeout=timeout) as response:
                            if response.status == 200:
                                return await response.json()
                            elif response.status == 429:
                                wait_time = random.uniform(10, 30)
                                logger.warning(f"触发限流，等待 {wait_time:.1f} 秒后重试")
                                await asyncio.sleep(wait_time)
                            else:
                                logger.warning(f"HTTP {response.status}: {url}")
                                
                except asyncio.TimeoutError:
                    logger.warning(f"请求超时 (尝试 {attempt + 1}/{self.max_retries})")
                except Exception as e:
                    logger.warning(f"请求异常 (尝试 {attempt + 1}/{self.max_retries}): {e}")
            
            return None

class AsyncNewsCrawler(AsyncFinancialCrawler):
    """异步新闻数据爬虫"""
    
    def __init__(self):
        super().__init__()
        self.base_url = config.URLS["news_search"]
    
    async def search_news(self, keyword: str, days: int = 7, max_results: int = 50) -> List[Dict]:
        """异步搜索新闻"""
        logger.info(f"开始异步搜索新闻: keyword={keyword}, days={days}")
        
        end_date = datetime.now()
        start_date = end_date - timedelta(days=days)
        
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
        
        text = await self._request_with_retry(self.base_url, params=params)
        if not text:
            return []
            
        try:
            json_str = re.search(r'\((.*)\)', text)
            if json_str:
                data = json.loads(json_str.group(1))
            else:
                data = {}
            
            news_list = self.news_parser.parse_eastmoney_news(data, keyword)
            
            cutoff = start_date.strftime("%Y-%m-%d")
            news_list = [
                n for n in news_list 
                if n.get('pub_date', '').replace('-', '').replace(' ', '') >= cutoff.replace('-', '')
            ]
            
            logger.info(f"异步搜索完成，获取到 {len(news_list)} 条新闻")
            return news_list
            
        except Exception as e:
            logger.error(f"异步搜索新闻解析失败: {e}")
            return []

    async def search_stock_news(self, stock_code: str, stock_name: str = "", days: int = 7) -> List[Dict]:
        """异步搜索指定股票新闻"""
        keyword = stock_name if stock_name else stock_code
        news_list = await self.search_news(keyword, days)
        
        for news in news_list:
            if not news.get('stock_code'):
                news['stock_code'] = stock_code
            if not news.get('stock_name') and stock_name:
                news['stock_name'] = stock_name
        
        return news_list

class AsyncFundCrawler(AsyncFinancialCrawler):
    """异步资金流向数据爬虫"""
    
    def __init__(self):
        super().__init__()
        self.base_url = config.URLS["fund_flow"]
    
    async def get_fund_flow(self, stock_code: str, days: int = 30) -> List[Dict]:
        """异步获取股票资金流向"""
        secid = config.format_stock_code(stock_code)
        
        params = {
            "lmt": days,
            "klt": 101,
            "secid": secid,
            "fields1": "f1,f2,f3,f7",
            "fields2": "f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61,f62,f63",
        }
        
        data = await self._request_with_retry(self.base_url, params=params)
        if not data:
            return []
            
        try:
            if isinstance(data, str):
                data = json.loads(data)
            funds_list = self.fund_parser.parse_eastmoney_funds(data, stock_code)
            return funds_list
        except Exception as e:
            logger.error(f"异步获取资金流向失败: {e}")
            return []

class AsyncSectorCrawler(AsyncFinancialCrawler):
    """异步行业板块数据爬虫"""
    
    def __init__(self):
        super().__init__()
        self.base_url = config.URLS["sector_fund_flow"]
        
    async def get_sector_fund_flow(self, trade_date: str = "") -> List[Dict]:
        """异步获取行业板块资金流向"""
        if not trade_date:
            trade_date = datetime.now().strftime("%Y-%m-%d")
            
        params = {
            "pn": 1,
            "pz": 200,
            "po": 1,
            "np": 1,
            "fltt": 2,
            "invt": 2,
            "fid": "f3",
            "fs": "m:90+t:2",
            "fields": "f1,f2,f3,f4,f12,f13,f14",
        }
        
        data = await self._request_with_retry(self.base_url, params=params)
        if not data:
            return []
            
        try:
            if isinstance(data, str):
                data = json.loads(data)
            sectors_list = self.sector_parser.parse_eastmoney_sectors(data, trade_date)
            return sectors_list
        except Exception as e:
            logger.error(f"异步获取行业板块失败: {e}")
            return []

# 便捷函数
async def batch_crawl_stocks(stock_codes: List[str], days: int = 7) -> Dict[str, Any]:
    """批量爬取多只股票数据"""
    crawler = AsyncNewsCrawler()
    tasks = []
    for code in stock_codes:
        tasks.append(crawler.search_stock_news(code, days=days))
    
    results = await asyncio.gather(*tasks)
    return dict(zip(stock_codes, results))

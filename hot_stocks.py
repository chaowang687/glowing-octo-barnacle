#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
热点股票数据获取模块
获取市场热点股票、热门概念板块数据
"""

import requests
import pandas as pd
import time
from typing import List, Dict


class HotStockSource:
    """热点股票数据源 - 优先东方财富，失败时使用腾讯"""
    
    def __init__(self):
        self.proxies = {
            'http': 'http://127.0.0.1:7890',
            'https': 'http://127.0.0.1:7890'
        }
        self.headers = {
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
            'Referer': 'https://quote.eastmoney.com/'
        }
    
    def get_all_a_stocks(self) -> List[str]:
        """
        获取全部A股股票代码列表
        
        Returns:
            List[str]: 股票代码列表，如 ['600000', '000001', '300001']
        """
        stocks = []
        
        # 沪市A股 (6开头)
        for i in range(600000, 603999):
            stocks.append(f'6{i:05d}')
        
        # 深市主板 (0开头)
        for i in range(1, 1000):
            stocks.append(f'0{i:04d}')
        
        # 创业板 (3开头)
        for i in range(1, 1000):
            stocks.append(f'3{i:04d}')
        
        # 北交所 (8, 4开头)
        for i in range(1, 500):
            stocks.append(f'8{i:04d}')
        for i in range(1, 500):
            stocks.append(f'4{i:04d}')
        
        return stocks
    
    def get_stock_list(self) -> pd.DataFrame:
        """
        获取完整股票列表（东方财富接口）
        
        Returns:
            DataFrame: 股票列表
        """
        url = "https://48.push2.eastmoney.com/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': 5000,
            'po': 1,
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f3',
            'fs': 'm:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23',
            'fields': 'f1,f2,f3,f4,f12,f13,f14'
        }
        
        try:
            resp = requests.get(url, params=params, headers=self.headers,
                              proxies=self.proxies, timeout=30)
            
            if resp.status_code == 200:
                data = resp.json()
                if 'data' in data and 'diff' in data['data']:
                    stocks = []
                    for item in data['data']['diff']:
                        code = str(item.get('f12', ''))
                        name = item.get('f14', '')
                        change = item.get('f3', 0)
                        
                        if code:
                            # 确定市场
                            if code.startswith('6'):
                                market = '沪市'
                            elif code.startswith('0') or code.startswith('3'):
                                market = '深市'
                            elif code.startswith('8') or code.startswith('4'):
                                market = '北交所'
                            else:
                                market = '其他'
                            
                            stocks.append({
                                '代码': code,
                                '名称': name,
                                '涨跌幅': change,
                                '市场': market
                            })
                    
                    return pd.DataFrame(stocks)
        except Exception as e:
            print(f"获取股票列表失败: {e}")
        
        return pd.DataFrame()
    
    def get_hot_concepts(self, limit: int = 10) -> pd.DataFrame:
        """
        获取热门概念板块
        
        Args:
            limit: 返回数量
            
        Returns:
            DataFrame: 热门概念板块
        """
        url = "https://push2.eastmoney.com/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': limit,
            'po': 1,
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f2',
            'fs': 'm:90+t:2',
            'fields': 'f2,f3,f4,f12,f14'
        }
        
        try:
            resp = requests.get(url, params=params, headers=self.headers,
                              proxies=self.proxies, timeout=30)
            
            if resp.status_code == 200:
                data = resp.json()
                if 'data' in data and 'diff' in data['data']:
                    concepts = []
                    for item in data['data']['diff']:
                        concepts.append({
                            '代码': item.get('f12', ''),
                            '名称': item.get('f14', ''),
                            '涨跌幅': item.get('f3', 0),
                            '净流入': item.get('f4', 0)
                        })
                    
                    return pd.DataFrame(concepts)
        except Exception as e:
            print(f"获取热门概念失败: {e}")
        
        return pd.DataFrame()
    
    def get_concept_stocks(self, concept_code: str) -> pd.DataFrame:
        """
        获取概念板块成分股
        
        Args:
            concept_code: 概念板块代码
            
        Returns:
            DataFrame: 成分股列表
        """
        url = f"https://push2.eastmoney.com/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': 100,
            'po': 1,
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f3',
            'fs': f'b:{concept_code}',
            'fields': 'f2,f3,f4,f12,f14'
        }
        
        try:
            resp = requests.get(url, params=params, headers=self.headers,
                              proxies=self.proxies, timeout=30)
            
            if resp.status_code == 200:
                data = resp.json()
                if 'data' in data and 'diff' in data['data']:
                    stocks = []
                    for item in data['data']['diff']:
                        stocks.append({
                            '代码': item.get('f12', ''),
                            '名称': item.get('f14', ''),
                            '最新价': item.get('f2', 0),
                            '涨跌幅': item.get('f3', 0)
                        })
                    
                    return pd.DataFrame(stocks)
        except Exception as e:
            print(f"获取概念成分股失败: {e}")
        
        return pd.DataFrame()
    
    def get_hot_stocks_realtime(self, limit: int = 50) -> pd.DataFrame:
        """
        获取实时热点股票（涨跌幅排行）
        
        Args:
            limit: 返回数量
            
        Returns:
            DataFrame: 热点股票
        """
        url = "https://push2.eastmoney.com/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': limit,
            'po': 1,
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f3',
            'fs': 'm:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23',
            'fields': 'f2,f3,f4,f12,f14,f62'
        }
        
        try:
            resp = requests.get(url, params=params, headers=self.headers,
                              proxies=self.proxies, timeout=30)
            
            if resp.status_code == 200:
                data = resp.json()
                if 'data' in data and 'diff' in data['data']:
                    stocks = []
                    for item in data['data']['diff']:
                        stocks.append({
                            '代码': item.get('f12', ''),
                            '名称': item.get('f14', ''),
                            '最新价': item.get('f2', 0),
                            '涨跌幅': item.get('f3', 0),
                            '成交额': item.get('f62', 0)
                        })
                    
                    return pd.DataFrame(stocks)
        except Exception as e:
            print(f"获取热点股票失败: {e}")
        
        return pd.DataFrame()
    
    def get_turnover_leaders(self, limit: int = 50) -> pd.DataFrame:
        """
        获取换手率排行
        
        Args:
            limit: 返回数量
            
        Returns:
            DataFrame: 高换手率股票
        """
        url = "https://push2.eastmoney.com/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': limit,
            'po': 1,
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f8',
            'fs': 'm:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23',
            'fields': 'f2,f3,f4,f8,f12,f14,f62'
        }
        
        try:
            resp = requests.get(url, params=params, headers=self.headers,
                              proxies=self.proxies, timeout=30)
            
            if resp.status_code == 200:
                data = resp.json()
                if 'data' in data and 'diff' in data['data']:
                    stocks = []
                    for item in data['data']['diff']:
                        stocks.append({
                            '代码': item.get('f12', ''),
                            '名称': item.get('f14', ''),
                            '最新价': item.get('f2', 0),
                            '涨跌幅': item.get('f3', 0),
                            '换手率': item.get('f8', 0),
                            '成交额': item.get('f62', 0)
                        })
                    
                    return pd.DataFrame(stocks)
        except Exception as e:
            print(f"获取换手率排行失败: {e}")
        
        return pd.DataFrame()
    
    def get_amount_leaders(self, limit: int = 50) -> pd.DataFrame:
        """
        获取成交额排行
        
        Args:
            limit: 返回数量
            
        Returns:
            DataFrame: 成交额排行股票
        """
        url = "https://push2.eastmoney.com/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': limit,
            'po': 1,
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f62',
            'fs': 'm:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23',
            'fields': 'f2,f3,f4,f12,f14,f62'
        }
        
        try:
            resp = requests.get(url, params=params, headers=self.headers,
                              proxies=self.proxies, timeout=30)
            
            if resp.status_code == 200:
                data = resp.json()
                if 'data' in data and 'diff' in data['data']:
                    stocks = []
                    for item in data['data']['diff']:
                        # 成交额单位转换
                        amount = item.get('f62', 0)
                        if amount > 100000000:
                            amount_str = f"{amount/100000000:.1f}亿"
                        elif amount > 10000:
                            amount_str = f"{amount/10000:.0f}万"
                        else:
                            amount_str = str(amount)
                        
                        stocks.append({
                            '代码': item.get('f12', ''),
                            '名称': item.get('f14', ''),
                            '最新价': item.get('f2', 0),
                            '涨跌幅': item.get('f3', 0),
                            '成交额': amount_str
                        })
                    
                    return pd.DataFrame(stocks)
        except Exception as e:
            print(f"获取成交额排行失败: {e}")
        
        return pd.DataFrame()


def get_stock_list() -> pd.DataFrame:
    """便捷函数：获取全部股票列表"""
    source = HotStockSource()
    return source.get_stock_list()


def get_hot_stocks(limit: int = 50) -> pd.DataFrame:
    """便捷函数：获取热点股票"""
    source = HotStockSource()
    return source.get_hot_stocks_realtime(limit)


def get_concept_stocks(concept_code: str) -> pd.DataFrame:
    """便捷函数：获取概念成分股"""
    source = HotStockSource()
    return source.get_concept_stocks(concept_code)


# 测试
if __name__ == '__main__':
    print("=" * 50)
    print("热点股票数据源测试")
    print("=" * 50)
    
    source = HotStockSource()
    
    # 测试1: 获取股票列表
    print("\n1. 获取股票列表:")
    stocks = source.get_stock_list()
    print(f"共获取 {len(stocks)} 只股票")
    if len(stocks) > 0:
        print(stocks.head(10))
    
    # 测试2: 热点股票
    print("\n2. 获取热点股票:")
    hot = source.get_hot_stocks_realtime(20)
    if len(hot) > 0:
        print(hot)
    
    # 测试3: 热门概念
    print("\n3. 获取热门概念:")
    concepts = source.get_hot_concepts(10)
    if len(concepts) > 0:
        print(concepts)
    
    print("\n✅ 测试完成")

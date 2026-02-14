#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
股票代码查询系统
功能：输入股票名称，得到对应的股票代码
支持从多个数据源获取股票代码数据
"""

import requests
from bs4 import BeautifulSoup
import json
from typing import List, Dict, Optional
import time
import os


class StockCodeLookup:
    """
    股票代码查询类
    支持从多个数据源查询股票代码
    """
    
    def __init__(self):
        """
        初始化股票代码查询类
        """
        self.headers = {
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
        }
        self.cache_file = os.path.join(os.path.dirname(__file__), 'stock_code_cache.json')
        self.cache_expiry = 86400  # 缓存过期时间，24小时
        self.cache = self._load_cache()
    
    def _load_cache(self) -> Dict:
        """
        加载缓存数据
        
        Returns:
            缓存数据字典
        """
        try:
            if os.path.exists(self.cache_file):
                with open(self.cache_file, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    # 检查缓存是否过期
                    if time.time() - data.get('timestamp', 0) < self.cache_expiry:
                        return data.get('data', {})
        except Exception as e:
            print(f"加载缓存失败: {e}")
        return {}
    
    def _save_cache(self, data: Dict):
        """
        保存缓存数据
        
        Args:
            data: 要缓存的数据
        """
        try:
            cache_data = {
                'timestamp': time.time(),
                'data': data
            }
            with open(self.cache_file, 'w', encoding='utf-8') as f:
                json.dump(cache_data, f, ensure_ascii=False, indent=2)
        except Exception as e:
            print(f"保存缓存失败: {e}")
    
    def lookup_by_name(self, stock_name: str) -> List[Dict]:
        """
        根据股票名称查询股票代码
        
        Args:
            stock_name: 股票名称
            
        Returns:
            股票代码列表，每个元素包含股票名称、代码、市场等信息
        """
        if not stock_name:
            return []
        
        # 先检查缓存
        cache_key = stock_name.strip()
        if cache_key in self.cache:
            print(f"从缓存中获取{stock_name}的股票代码")
            return self.cache[cache_key]
        
        print(f"查询{stock_name}的股票代码...")
        
        # 从多个数据源查询
        results = []
        
        # 1. 从东方财富查询
        eastmoney_results = self._lookup_eastmoney(stock_name)
        if eastmoney_results:
            results.extend(eastmoney_results)
        
        # 2. 从新浪财经查询
        sina_results = self._lookup_sina(stock_name)
        if sina_results:
            results.extend(sina_results)
        
        # 3. 从同花顺查询
        if not results:
            toujiao_results = self._lookup_toujiao(stock_name)
            if toujiao_results:
                results.extend(toujiao_results)
        
        # 4. 从雪球查询
        if not results:
            xueqiu_results = self._lookup_xueqiu(stock_name)
            if xueqiu_results:
                results.extend(xueqiu_results)
        
        # 去重
        unique_results = self._deduplicate_results(results)
        
        # 保存到缓存
        if unique_results:
            self.cache[cache_key] = unique_results
            self._save_cache(self.cache)
        
        print(f"查询完成，找到{len(unique_results)}个匹配结果")
        return unique_results
    
    def _lookup_eastmoney(self, stock_name: str) -> List[Dict]:
        """
        从东方财富查询股票代码
        
        Args:
            stock_name: 股票名称
            
        Returns:
            股票代码列表
        """
        results = []
        try:
            # 东方财富搜索接口
            url = f"https://searchapi.eastmoney.com/api/suggest/get?input={stock_name}&type=14&token=D43BF722C8E33BDC906FB84D85E326E8"
            resp = requests.get(url, headers=self.headers, timeout=10)
            resp.raise_for_status()
            
            data = resp.json()
            if data.get('QuotationCodeTable'):
                for item in data['QuotationCodeTable'].get('Data', []):
                    code = item.get('Code', '')
                    name = item.get('Name', '')
                    market = item.get('Market', '')
                    if code and name:
                        results.append({
                            'name': name,
                            'code': code,
                            'market': market,
                            'source': '东方财富'
                        })
            print(f"东方财富查询到{len(results)}个结果")
        except Exception as e:
            print(f"东方财富查询失败: {e}")
        return results
    
    def _lookup_sina(self, stock_name: str) -> List[Dict]:
        """
        从新浪财经查询股票代码
        
        Args:
            stock_name: 股票名称
            
        Returns:
            股票代码列表
        """
        results = []
        try:
            # 新浪财经搜索接口
            url = f"https://suggest3.sinajs.cn/suggest/type=11,12&key={stock_name}"
            resp = requests.get(url, headers=self.headers, timeout=10)
            resp.raise_for_status()
            
            content = resp.text
            # 解析新浪财经的返回格式
            # 格式示例: var sug_11=";600519,贵州茅台,sh;600543,莫高股份,sh"
            if '=' in content:
                data_part = content.split('=')[1].strip('"')
                items = data_part.split(';')
                for item in items:
                    if item:
                        parts = item.split(',')
                        if len(parts) >= 3:
                            # 确保格式正确，code应该是数字，name应该是中文
                            code = parts[0]
                            name = parts[1]
                            market = parts[2]
                            
                            # 验证code是否为数字
                            if code.isdigit():
                                results.append({
                                    'name': name,
                                    'code': code,
                                    'market': market,
                                    'source': '新浪财经'
                                })
            print(f"新浪财经查询到{len(results)}个结果")
        except Exception as e:
            print(f"新浪财经查询失败: {e}")
        return results
    
    def _lookup_toujiao(self, stock_name: str) -> List[Dict]:
        """
        从同花顺查询股票代码
        
        Args:
            stock_name: 股票名称
            
        Returns:
            股票代码列表
        """
        results = []
        try:
            # 同花顺搜索接口
            url = f"http://suggest3.10jqka.com.cn/suggest?key={stock_name}&type=1"
            resp = requests.get(url, headers=self.headers, timeout=10)
            resp.raise_for_status()
            
            data = resp.json()
            if data.get('items'):
                for item in data['items']:
                    if len(item) >= 3:
                        code, name, _ = item
                        # 确定市场
                        market = 'sh' if code.startswith('6') else 'sz'
                        results.append({
                            'name': name,
                            'code': code,
                            'market': market,
                            'source': '同花顺'
                        })
            print(f"同花顺查询到{len(results)}个结果")
        except Exception as e:
            print(f"同花顺查询失败: {e}")
        return results
    
    def _lookup_xueqiu(self, stock_name: str) -> List[Dict]:
        """
        从雪球查询股票代码
        
        Args:
            stock_name: 股票名称
            
        Returns:
            股票代码列表
        """
        results = []
        try:
            # 雪球搜索接口
            url = f"https://xueqiu.com/stock/search.json?code={stock_name}"
            resp = requests.get(url, headers=self.headers, timeout=10)
            resp.raise_for_status()
            
            data = resp.json()
            if data.get('stocks'):
                for item in data['stocks']:
                    code = item.get('code', '')
                    name = item.get('name', '')
                    if code and name:
                        # 处理雪球的代码格式，如SH600519
                        if code.startswith('SH'):
                            market = 'sh'
                            pure_code = code[2:]
                        elif code.startswith('SZ'):
                            market = 'sz'
                            pure_code = code[2:]
                        else:
                            market = 'unknown'
                            pure_code = code
                        
                        results.append({
                            'name': name,
                            'code': pure_code,
                            'market': market,
                            'source': '雪球'
                        })
            print(f"雪球查询到{len(results)}个结果")
        except Exception as e:
            print(f"雪球查询失败: {e}")
        return results
    
    def _deduplicate_results(self, results: List[Dict]) -> List[Dict]:
        """
        去重查询结果
        
        Args:
            results: 查询结果列表
            
        Returns:
            去重后的结果列表
        """
        seen = set()
        unique_results = []
        
        for result in results:
            code = result.get('code', '')
            name = result.get('name', '')
            if code and name:
                key = (code, name)
                if key not in seen:
                    seen.add(key)
                    unique_results.append(result)
        
        return unique_results
    
    def get_full_stock_info(self, code: str) -> Optional[Dict]:
        """
        根据股票代码获取完整的股票信息
        
        Args:
            code: 股票代码
            
        Returns:
            股票信息字典
        """
        try:
            # 使用东方财富获取完整信息
            url = f"https://emweb.securities.eastmoney.com/PC_HSF10/CompanySurvey/Index?type=soft&code={code}"
            resp = requests.get(url, headers=self.headers, timeout=10)
            resp.raise_for_status()
            
            soup = BeautifulSoup(resp.text, 'html.parser')
            
            # 提取股票信息
            info = {
                'code': code,
                'name': '',
                'industry': '',
                'area': '',
                'market': '',
                'listing_date': ''
            }
            
            # 这里需要根据实际页面结构解析
            # 示例：查找公司名称
            name_elem = soup.find('h1', class_=['company-name', 'name'])
            if name_elem:
                info['name'] = name_elem.text.strip()
            
            return info
        except Exception as e:
            print(f"获取股票完整信息失败: {e}")
            return None


def get_stock_code_lookup() -> StockCodeLookup:
    """
    获取股票代码查询实例
    
    Returns:
        StockCodeLookup实例
    """
    return StockCodeLookup()


# 测试
if __name__ == '__main__':
    lookup = StockCodeLookup()
    
    # 测试股票名称查询
    test_names = ['贵州茅台', '五粮液', '中国平安', '比亚迪', '宁德时代']
    
    for name in test_names:
        print(f"\n测试查询: {name}")
        results = lookup.lookup_by_name(name)
        for result in results:
            print(f"名称: {result['name']}, 代码: {result['code']}, 市场: {result['market']}, 来源: {result['source']}")

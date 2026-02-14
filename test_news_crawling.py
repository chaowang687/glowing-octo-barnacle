#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试新闻抓取功能
验证各个新闻源是否能够正常获取新闻
"""

import requests
import json
from market_analyzer import get_market_analyzer

def test_news_sources():
    """
    测试各个新闻源
    """
    print("=" * 60)
    print("测试新闻抓取功能")
    print("=" * 60)
    
    analyzer = get_market_analyzer()
    
    # 测试股票
    test_stocks = [
        ('300251', '光线传媒'),
        ('600519', '贵州茅台'),
        ('000001', '平安银行')
    ]
    
    for symbol, name in test_stocks:
        print(f"\n{'=' * 50}")
        print(f"测试股票: {name} ({symbol})")
        print(f"{'=' * 50}")
        
        # 测试1: 测试东方财富新闻API
        print("\n1. 测试东方财富新闻API:")
        try:
            headers = {
                'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
            }
            
            # 东方财富新闻接口
            url = f"http://api.eastmoney.com/news/list"
            params = {
                'type': 'stock',
                'id': symbol,
                'pageSize': 20,
                'pageIndex': 1
            }
            
            resp = requests.get(url, params=params, headers=headers, timeout=10)
            print(f"状态码: {resp.status_code}")
            print(f"响应长度: {len(resp.text)}")
            
            # 打印完整响应内容的前500字符
            print(f"响应内容: {resp.text[:500]}...")
            
            # 尝试解析JSON
            try:
                data = resp.json()
                print(f"JSON解析成功")
                print(f"数据结构: {list(data.keys())}")
                if 'items' in data:
                    print(f"新闻条数: {len(data['items'])}")
                    if data['items']:
                        print(f"第一条新闻标题: {data['items'][0].get('title', '')}")
            except json.JSONDecodeError as e:
                print(f"JSON解析失败: {e}")
                
        except Exception as e:
            print(f"请求失败: {e}")
        
        # 测试2: 测试完整的新闻获取
        print("\n2. 测试完整的新闻获取:")
        try:
            news = analyzer.get_stock_news(symbol, days=7)
            print(f"获取到新闻条数: {len(news)}")
            if news:
                print("最近5条新闻:")
                for i, item in enumerate(news[:5], 1):
                    title = item.get('title', '')
                    time_str = item.get('time', '')
                    source = item.get('source', '')
                    print(f"{i}. [{time_str}] {title[:50]}... (来源: {source})")
            else:
                print("未获取到新闻")
        except Exception as e:
            print(f"获取新闻失败: {e}")
        
        # 测试3: 测试利好利空分析
        print("\n3. 测试利好利空分析:")
        try:
            factors = analyzer.analyze_factors(symbol)
            print(f"利好因素: {len(factors['bullish'])}条")
            if factors['bullish']:
                print("主要利好因素:")
                for factor in factors['bullish'][:3]:
                    print(f"- {factor}")
            
            print(f"利空因素: {len(factors['bearish'])}条")
            if factors['bearish']:
                print("主要利空因素:")
                for factor in factors['bearish'][:3]:
                    print(f"- {factor}")
            
            print(f"行业热点: {len(factors['industry_hotspots'])}条")
            if factors['industry_hotspots']:
                print("主要行业热点:")
                for hotspot in factors['industry_hotspots'][:3]:
                    print(f"- {hotspot}")
                    
        except Exception as e:
            print(f"分析利好利空失败: {e}")
    
    print("\n" + "=" * 60)
    print("测试完成")
    print("=" * 60)

if __name__ == '__main__':
    test_news_sources()

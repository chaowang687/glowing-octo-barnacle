#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试新闻爬虫功能
"""

from market_analyzer import get_market_analyzer

if __name__ == '__main__':
    print("=" * 60)
    print("测试新闻爬虫功能")
    print("=" * 60)
    
    # 测试光线传媒
    symbol = '300251'
    name = '光线传媒'
    
    print(f"\n测试股票: {name} ({symbol})")
    
    analyzer = get_market_analyzer()
    
    # 测试获取新闻
    print("\n1. 测试获取新闻...")
    news = analyzer.get_stock_news(symbol, days=7, name=name)
    print(f"获取到 {len(news)} 条新闻")
    for i, item in enumerate(news[:10]):
        print(f"  {i+1}. {item['title']} - {item['time']} - {item['source']}")
    
    # 测试利好利空分析
    print("\n2. 测试利好利空分析...")
    factors = analyzer.analyze_factors(symbol, name=name)
    print(f"利好因素: {len(factors['bullish'])}条")
    for factor in factors['bullish'][:5]:
        print(f"  - {factor}")
    print(f"利空因素: {len(factors['bearish'])}条")
    for factor in factors['bearish'][:5]:
        print(f"  - {factor}")
    print(f"行业热点: {len(factors['industry_hotspots'])}条")
    for hotspot in factors['industry_hotspots'][:5]:
        print(f"  - {hotspot}")
    print(f"市场趋势: {len(factors['market_trends'])}条")
    for trend in factors['market_trends'][:5]:
        print(f"  - {trend}")

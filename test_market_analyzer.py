#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试市场信息分析功能
"""

from market_analyzer import get_market_analyzer

if __name__ == '__main__':
    print("=" * 60)
    print("测试市场信息分析功能")
    print("=" * 60)
    
    # 测试光线传媒
    symbol = '300251'
    name = '光线传媒'
    
    print(f"\n分析股票: {name} ({symbol})")
    
    analyzer = get_market_analyzer()
    
    # 测试获取新闻
    print("\n1. 测试获取新闻...")
    news = analyzer.get_stock_news(symbol, days=7, name=name)
    print(f"获取到 {len(news)} 条新闻")
    for i, item in enumerate(news[:5]):
        print(f"  {i+1}. {item['title']} - {item['time']} - {item['source']}")
    
    # 测试利好利空分析
    print("\n2. 测试利好利空分析...")
    factors = analyzer.analyze_factors(symbol, name=name)
    print(f"利好因素: {factors['bullish']}")
    print(f"利空因素: {factors['bearish']}")
    print(f"行业热点: {factors['industry_hotspots']}")
    print(f"市场趋势: {factors['market_trends']}")
    
    # 测试综合分析
    print("\n3. 测试综合分析...")
    analysis = analyzer.comprehensive_analysis(symbol, name)
    print(f"综合分析完成，新闻数量: {len(analysis['news'])}")
    print(f"主力资金净流入: {analysis['main_funds']['net_inflow']/10000:.2f}万")
    print(f"资金状态: {analysis['main_funds']['status']}")

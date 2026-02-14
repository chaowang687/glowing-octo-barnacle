#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试应用程序中的市场信息分析功能
"""

from market_analyzer import get_market_analyzer

if __name__ == '__main__':
    print("=" * 60)
    print("测试应用程序中的市场信息分析功能")
    print("=" * 60)
    
    # 测试光线传媒
    symbol = '300251'
    name = '光线传媒'
    
    print(f"\n分析股票: {name} ({symbol})")
    
    analyzer = get_market_analyzer()
    
    # 测试综合分析
    print("\n1. 测试综合分析...")
    analysis = analyzer.comprehensive_analysis(symbol, name)
    print(f"综合分析完成，新闻数量: {len(analysis['news'])}")
    print(f"主力资金净流入: {analysis['main_funds']['net_inflow']/10000:.2f}万")
    print(f"资金状态: {analysis['main_funds']['status']}")
    
    # 显示利好利空分析
    print("\n2. 测试利好利空分析...")
    factors = analysis.get('factors', {})
    bullish = factors.get('bullish', [])
    bearish = factors.get('bearish', [])
    industry_hotspots = factors.get('industry_hotspots', [])
    market_trends = factors.get('market_trends', [])
    
    print(f"利好因素: {len(bullish)}条")
    for factor in bullish[:5]:
        print(f"  - {factor}")
    
    print(f"利空因素: {len(bearish)}条")
    for factor in bearish[:5]:
        print(f"  - {factor}")
    
    print(f"行业热点: {len(industry_hotspots)}条")
    for hotspot in industry_hotspots[:5]:
        print(f"  - {hotspot}")
    
    print(f"市场趋势: {len(market_trends)}条")
    for trend in market_trends[:5]:
        print(f"  - {trend}")
    
    # 测试获取新闻
    print("\n3. 测试获取新闻...")
    news = analyzer.get_stock_news(symbol, days=7, name=name)
    print(f"获取到 {len(news)} 条新闻")
    for i, item in enumerate(news[:5]):
        print(f"  {i+1}. {item['title']} - {item['time']} - {item['source']}")

#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试MarketAnalyzer是否正确集成了FinNewsCrawler_v2
"""

import sys
import os

# 添加当前目录到路径
sys.path.insert(0, os.path.dirname(__file__))

from market_analyzer import get_market_analyzer

def test_market_analyzer():
    """测试MarketAnalyzer"""
    print("=" * 60)
    print("测试MarketAnalyzer集成FinNewsCrawler_v2")
    print("=" * 60)
    
    # 获取MarketAnalyzer实例
    analyzer = get_market_analyzer()
    
    # 测试光线传媒
    symbol = '300251'
    name = '光线传媒'
    
    print(f"\n分析股票: {name} ({symbol})")
    
    # 测试获取新闻
    print("\n1. 测试获取新闻...")
    news = analyzer.get_stock_news(symbol, days=7)
    print(f"获取到{len(news)}条新闻")
    for i, item in enumerate(news[:5]):
        print(f"{i+1}. {item['title']} - {item['time']} ({item['source']})")
    
    # 测试利好利空分析
    print("\n2. 测试利好利空分析...")
    factors = analyzer.analyze_factors(symbol)
    print(f"利好因素: {len(factors['bullish'])}条")
    for factor in factors['bullish'][:3]:
        print(f"  - {factor}")
    print(f"利空因素: {len(factors['bearish'])}条")
    for factor in factors['bearish'][:3]:
        print(f"  - {factor}")
    print(f"行业热点: {len(factors['industry_hotspots'])}条")
    for hotspot in factors['industry_hotspots'][:3]:
        print(f"  - {hotspot}")
    print(f"市场趋势: {len(factors['market_trends'])}条")
    for trend in factors['market_trends']:
        print(f"  - {trend}")
    
    # 测试主力资金
    print("\n3. 测试主力资金流向...")
    funds = analyzer.get_main_funds(symbol)
    print(f"主力资金净流入: {funds['net_inflow']/10000:.2f}万")
    print(f"资金状态: {funds['status']}")
    print("每日数据:")
    for day in funds['daily_data'][:3]:
        print(f"  {day['date']}: 流入{day['inflow']/10000:.2f}万, 流出{day['outflow']/10000:.2f}万, 净额{day['net']/10000:.2f}万")
    
    # 测试综合分析
    print("\n4. 测试综合分析...")
    analysis = analyzer.comprehensive_analysis(symbol, name)
    print(f"综合分析完成，包含以下数据:")
    print(f"- 新闻数量: {len(analysis['news'])}")
    print(f"- 利好因素: {len(analysis['factors']['bullish'])}")
    print(f"- 利空因素: {len(analysis['factors']['bearish'])}")
    print(f"- 行业热点: {len(analysis['factors']['industry_hotspots'])}")
    print(f"- 市场趋势: {len(analysis['factors']['market_trends'])}")
    print(f"- 主力资金净流入: {analysis['main_funds']['net_inflow']/10000:.2f}万")
    
    print("\n" + "=" * 60)
    print("测试完成")
    print("=" * 60)

if __name__ == '__main__':
    test_market_analyzer()

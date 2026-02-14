#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试 FinNewsCrawler_v2 集成到主项目
功能：验证 market_analyzer.py 是否能成功导入并使用 FinNewsCrawler_v2
"""

import sys
import os

# 添加当前目录到路径
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from market_analyzer import MarketAnalyzer, FINNEWS_AVAILABLE

def test_finnews_integration():
    """
    测试 FinNewsCrawler_v2 集成
    """
    print("=" * 70)
    print("测试 FinNewsCrawler_v2 集成到主项目")
    print("=" * 70)
    
    # 显示集成状态
    print(f"FinNewsCrawler_v2 可用状态: {FINNEWS_AVAILABLE}")
    
    # 创建 MarketAnalyzer 实例
    analyzer = MarketAnalyzer()
    print("MarketAnalyzer 实例创建成功")
    
    # 测试光线传媒
    symbol = '300251'
    name = '光线传媒'
    
    print(f"\n测试分析股票: {name} ({symbol})")
    
    # 测试获取新闻
    print("\n1. 测试获取新闻...")
    news = analyzer.get_stock_news(symbol, days=3)
    print(f"获取到 {len(news)} 条新闻")
    for i, item in enumerate(news[:5]):  # 显示前5条
        print(f"  {i+1}. {item.get('time', '')[:10]} - {item.get('title', '')[:50]}...")
    
    # 测试主力资金流向
    print("\n2. 测试获取主力资金流向...")
    funds = analyzer.get_main_funds(symbol, days=3)
    print(f"主力资金净流入: {funds['net_inflow']/10000:.2f}万")
    print(f"资金状态: {funds['status']}")
    print("每日数据:")
    for day in funds['daily_data']:
        print(f"  {day['date']}: 净流入 {day['net']/10000:.2f}万")
    
    # 测试综合分析
    print("\n3. 测试综合分析...")
    analysis = analyzer.comprehensive_analysis(symbol, name)
    print(f"综合分析完成，包含以下内容:")
    print(f"  - 新闻数量: {len(analysis.get('news', []))}")
    print(f"  - 利好因素: {len(analysis.get('factors', {}).get('bullish', []))}")
    print(f"  - 利空因素: {len(analysis.get('factors', {}).get('bearish', []))}")
    print(f"  - 主力资金状态: {analysis.get('main_funds', {}).get('status', 'unknown')}")
    
    # 测试利好利空分析
    print("\n4. 测试利好利空分析...")
    factors = analyzer.analyze_factors(symbol)
    print(f"利好因素数量: {len(factors['bullish'])}")
    print(f"利空因素数量: {len(factors['bearish'])}")
    print(f"行业热点数量: {len(factors['industry_hotspots'])}")
    
    print("\n" + "=" * 70)
    print("测试完成")
    print("=" * 70)

if __name__ == '__main__':
    test_finnews_integration()

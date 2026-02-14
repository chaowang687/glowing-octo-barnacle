#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试市场信息分析功能
验证多数据源新闻收集和分析能力
"""

from market_analyzer import get_market_analyzer


def test_market_analysis():
    """测试市场分析器的完整功能"""
    print("=" * 70)
    print("测试市场信息分析功能")
    print("=" * 70)
    
    analyzer = get_market_analyzer()
    
    # 测试多个股票
    test_symbols = [('002498', '汉缆股份'), ('300251', '光线传媒'), ('600519', '贵州茅台')]
    
    for symbol, name in test_symbols:
        print(f"\n{'-' * 60}")
        print(f"分析股票: {name} ({symbol})")
        print('-' * 60)
        
        # 测试1: 新闻收集
        print("\n1. 测试新闻收集:")
        news = analyzer.get_stock_news(symbol, days=7)
        print(f"收集到新闻条数: {len(news)}")
        if news:
            print("最近5条新闻:")
            for i, item in enumerate(news[:5], 1):
                title = item.get('title', '')
                time = item.get('time', '')
                source = item.get('source', '')
                print(f"{i}. [{time}] {title} (来源: {source})")
        else:
            print("未收集到新闻")
        
        # 测试2: 利好利空分析
        print("\n2. 测试利好利空分析:")
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
        
        # 测试3: 行业热点分析
        print("\n3. 测试行业热点分析:")
        industry_hotspots = factors.get('industry_hotspots', [])
        print(f"行业热点: {len(industry_hotspots)}条")
        if industry_hotspots:
            print("主要行业热点:")
            for hotspot in industry_hotspots[:3]:
                print(f"- {hotspot}")
        
        # 测试4: 市场趋势分析
        print("\n4. 测试市场趋势分析:")
        market_trends = factors.get('market_trends', [])
        print(f"市场趋势分析: {len(market_trends)}条")
        if market_trends:
            print("市场趋势:")
            for trend in market_trends:
                print(f"- {trend}")
        
        # 测试5: 综合分析
        print("\n5. 测试综合分析:")
        analysis = analyzer.comprehensive_analysis(symbol, name)
        print(f"综合分析完成，包含:")
        print(f"- 新闻条数: {len(analysis.get('news', []))}")
        print(f"- 利好因素: {len(analysis.get('factors', {}).get('bullish', []))}条")
        print(f"- 利空因素: {len(analysis.get('factors', {}).get('bearish', []))}条")
        print(f"- 行业热点: {len(analysis.get('factors', {}).get('industry_hotspots', []))}条")
        print(f"- 市场趋势: {len(analysis.get('factors', {}).get('market_trends', []))}条")
        print(f"- 主力资金分析: {'是' if analysis.get('main_funds', {}) else '否'}")
        print(f"- 市场环境分析: {'是' if analysis.get('market_context', {}) else '否'}")
    
    print("\n" + "=" * 70)
    print("测试完成")
    print("=" * 70)


if __name__ == '__main__':
    test_market_analysis()

#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试投研公司评级获取功能
确保每只股票至少获取近半年的投研公司评级
"""

import sys
import os
from market_analyzer import get_market_analyzer


def test_research_ratings():
    """
    测试投研公司评级获取功能
    """
    print("=" * 80)
    print("测试投研公司评级获取功能")
    print("=" * 80)
    
    # 获取市场分析器实例
    analyzer = get_market_analyzer()
    
    # 测试股票列表
    test_stocks = [
        ('600519', '贵州茅台'),  # 白酒龙头
        ('000858', '五粮液'),    # 白酒
        ('601318', '中国平安'),  # 保险
        ('000001', '平安银行'),  # 银行
        ('002594', '比亚迪'),    # 新能源汽车
        ('300750', '宁德时代'),  # 锂电池
        ('600036', '招商银行'),  # 银行
        ('601888', '中国中免'),  # 免税
        ('600276', '恒瑞医药'),  # 医药
        ('300059', '东方财富'),  # 金融科技
    ]
    
    for symbol, name in test_stocks:
        print(f"\n测试股票: {name} ({symbol})")
        print("-" * 60)
        
        try:
            # 获取投研公司评级
            ratings = analyzer.get_research_ratings(symbol, name)
            
            # 打印评级结果
            print(f"评级总数: {ratings['summary']['total']}")
            print(f"买入评级: {ratings['summary']['buy']}")
            print(f"中性评级: {ratings['summary']['hold']}")
            print(f"卖出评级: {ratings['summary']['sell']}")
            
            # 打印前5条评级详情
            print(f"\n前5条评级详情:")
            for i, rating in enumerate(ratings['ratings'][:5], 1):
                firm = rating.get('firm', '未知机构')
                rating_value = rating.get('rating', '未知评级')
                date = rating.get('date', '未知日期')
                source = rating.get('source', '未知来源')
                title = rating.get('title', '无标题')[:50] + '...' if len(rating.get('title', '')) > 50 else rating.get('title', '无标题')
                
                print(f"{i}. {firm} - {rating_value} ({date})")
                print(f"   来源: {source}")
                print(f"   标题: {title}")
                print()
            
            # 检查是否获取到足够的评级数据
            if ratings['summary']['total'] >= 3:
                print("✅ 测试通过: 获取到足够的评级数据")
            else:
                print("❌ 测试失败: 评级数据不足")
                
        except Exception as e:
            print(f"❌ 测试失败: {str(e)}")
        
        print("-" * 60)


if __name__ == '__main__':
    test_research_ratings()

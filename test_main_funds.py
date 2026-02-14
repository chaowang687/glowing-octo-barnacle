#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试个股主力资金分析功能
"""

import sys
sys.path.insert(0, '/Users/wangchao/Desktop/a_quant')

from market_analyzer import get_market_analyzer
import json

# 禁用代理
import os
for var in ['HTTP_PROXY', 'HTTPS_PROXY', 'http_proxy', 'https_proxy']:
    os.environ.pop(var, None)
os.environ['no_proxy'] = '*'

# 测试股票代码
test_stocks = ['600519', '000858', '300750', '601318']

print("=" * 60)
print("测试个股近五日主力资金、游资、散户资金分析功能")
print("=" * 60)

# 初始化分析器
analyzer = get_market_analyzer()

for symbol in test_stocks:
    print(f"\n{'='*60}")
    print(f"测试股票: {symbol}")
    print("=" * 60)
    
    try:
        # 获取主力资金数据
        funds = analyzer.get_main_funds(symbol, days=5)
        
        print(f"\n📊 主力资金分析结果:")
        print(f"  总流入: {funds.get('total_inflow', 0)/10000:.2f}万")
        print(f"  总流出: {funds.get('total_outflow', 0)/10000:.2f}万")
        print(f"  净流入: {funds.get('net_inflow', 0)/10000:.2f}万")
        
        # 显示状态
        net = funds.get('net_inflow', 0)
        if net > 0:
            status_text = "📈 流入"
        elif net < 0:
            status_text = "📉 流出"
        else:
            status_text = "⚖️ 平衡"
        print(f"  状态: {status_text}")
        
        print(f"\n📅 近5日资金流向:")
        daily_data = funds.get('daily_data', [])
        
        if daily_data:
            # 表头
            print(f"  {'日期':<12} {'主力资金':<15} {'游资资金':<15} {'散户资金':<15}")
            print(f"  {'-'*12} {'-'*15} {'-'*15} {'-'*15}")
            
            for day in daily_data:
                date = day.get('date', '')
                main_net = day.get('main_net', 0) / 10000
                hot_money = day.get('hot_money_net', 0) / 10000
                retail = day.get('retail_net', 0) / 10000
                
                print(f"  {date:<12} {main_net:>12.2f}万 {hot_money:>12.2f}万 {retail:>12.2f}万")
        else:
            print("  ⚠️ 暂无数据")
            
    except Exception as e:
        print(f"  ❌ 测试失败: {str(e)}")
        import traceback
        traceback.print_exc()

print("\n" + "=" * 60)
print("测试完成")
print("=" * 60)

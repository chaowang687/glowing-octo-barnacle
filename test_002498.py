#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试002498股票的数据获取情况
"""

import pandas as pd
from tencent_source import TencentDataSource
from data_source import EastMoneyData


def test_002498():
    """测试002498股票的数据"""
    print("=" * 60)
    print("测试002498股票数据获取")
    print("=" * 60)
    
    # 测试腾讯数据源
    print("\n1. 测试腾讯数据源:")
    tencent = TencentDataSource()
    
    # 测试实时行情
    print("\n1.1 实时行情:")
    realtime_data = tencent.get_realtime_quote(['002498'])
    if not realtime_data.empty:
        print(realtime_data)
    else:
        print("❌ 实时行情获取失败")
    
    # 测试K线数据
    print("\n1.2 K线数据:")
    kline_data = tencent.get_stock_kline('002498')
    if not kline_data.empty:
        print(f"K线数据条数: {len(kline_data)}")
        print(kline_data.tail())
    else:
        print("❌ K线数据获取失败")
    
    # 测试东方财富数据源
    print("\n2. 测试东方财富数据源:")
    em = EastMoneyData()
    
    # 测试K线数据
    print("\n2.1 K线数据:")
    em_kline = em.get_stock_kline('002498')
    if not em_kline.empty:
        print(f"K线数据条数: {len(em_kline)}")
        print(em_kline.tail())
    else:
        print("❌ K线数据获取失败")
    
    print("\n" + "=" * 60)
    print("测试完成")
    print("=" * 60)


if __name__ == '__main__':
    test_002498()

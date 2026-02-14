#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
测试腾讯数据源获取300251光线传媒的数据
"""

from tencent_source import TencentDataSource

if __name__ == '__main__':
    tencent = TencentDataSource()
    
    # 测试获取K线数据
    print("测试获取300251光线传媒K线数据...")
    kline = tencent.get_stock_kline('300251')
    
    if kline is not None:
        print(f"K线数据行数: {len(kline)}")
        if len(kline) > 0:
            print(f"最新价格: {kline['收盘'].iloc[-1]}")
            print(f"最新日期: {kline['日期'].iloc[-1]}")
    else:
        print("获取K线数据失败")
    
    # 测试获取实时行情
    print("\n测试获取300251光线传媒实时行情...")
    quote = tencent.get_realtime_quote(['300251'])
    if quote is not None:
        print(f"实时行情行数: {len(quote)}")
        if len(quote) > 0:
            print(quote.head())
    else:
        print("获取实时行情失败")

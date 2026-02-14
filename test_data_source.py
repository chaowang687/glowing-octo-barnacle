#!/usr/bin/env python3
"""
测试数据源是否正常工作
"""

import sys
from tencent_source import TencentDataSource

def test_tencent_source():
    """测试腾讯财经数据源"""
    print("=== 测试腾讯财经数据源 ===")
    
    try:
        # 初始化数据源
        tencent = TencentDataSource()
        print("✅ 腾讯数据源初始化成功")
        
        # 测试获取实时行情
        print("\n测试1: 获取实时行情")
        df = tencent.get_realtime_quotes(10)
        if df is not None and len(df) > 0:
            print(f"✅ 成功获取 {len(df)} 条实时行情数据")
            print("前5条数据:")
            print(df.head())
        else:
            print("❌ 无法获取实时行情数据")
        
        # 测试获取K线数据
        print("\n测试2: 获取K线数据")
        kline = tencent.get_stock_kline('600519')
        if kline is not None and len(kline) > 0:
            print(f"✅ 成功获取 {len(kline)} 条K线数据")
            print("最近5天K线:")
            print(kline.tail())
        else:
            print("❌ 无法获取K线数据")
        
        print("\n=== 测试完成 ===")
        return True
        
    except Exception as e:
        print(f"❌ 测试失败: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    success = test_tencent_source()
    sys.exit(0 if success else 1)

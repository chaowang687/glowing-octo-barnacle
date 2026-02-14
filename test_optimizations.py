#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试所有优化的代码是否正常运行
"""

import sys
import os

# 添加当前目录到Python路径
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))


def test_config_module():
    """测试配置管理模块"""
    print("测试配置管理模块...")
    try:
        from config import get_config, get_proxies, get_data_config, get_chanlun_config
        config = get_config()
        proxies = get_proxies()
        data_config = get_data_config()
        chanlun_config = get_chanlun_config()
        print(f"✓ 配置管理模块加载成功")
        print(f"  代理配置: {proxies}")
        print(f"  数据配置: {data_config}")
        print(f"  缠论配置: {chanlun_config}")
        return True
    except Exception as e:
        print(f"✗ 配置管理模块加载失败: {e}")
        return False


def test_indicators_module():
    """测试技术指标模块"""
    print("\n测试技术指标模块...")
    try:
        from indicators import calculate_ma, calculate_macd, calculate_kdj, calculate_obv, calculate_all_indicators
        import pandas as pd
        import numpy as np
        from datetime import datetime, timedelta
        
        # 创建测试数据
        dates = pd.date_range(end=datetime.now(), periods=100, freq='D')
        prices = np.cumsum(np.random.randn(100) * 0.5) + 100
        
        test_df = pd.DataFrame({
            '收盘': prices,
            '最高': prices * (1 + np.random.rand(100) * 0.02),
            '最低': prices * (1 - np.random.rand(100) * 0.02),
            '成交量': np.random.randint(1000, 10000, 100)
        }, index=dates)
        
        # 测试各个指标计算
        ma_df = calculate_ma(test_df)
        macd_df = calculate_macd(test_df)
        kdj_df = calculate_kdj(test_df)
        obv_df = calculate_obv(test_df)
        all_df = calculate_all_indicators(test_df)
        
        print(f"✓ 技术指标模块加载成功")
        print(f"  MA计算成功: {len(ma_df)} 条记录")
        print(f"  MACD计算成功: {len(macd_df)} 条记录")
        print(f"  KDJ计算成功: {len(kdj_df)} 条记录")
        print(f"  OBV计算成功: {len(obv_df)} 条记录")
        print(f"  全指标计算成功: {len(all_df)} 条记录")
        return True
    except Exception as e:
        print(f"✗ 技术指标模块加载失败: {e}")
        import traceback
        traceback.print_exc()
        return False


def test_logger_module():
    """测试日志管理模块"""
    print("\n测试日志管理模块...")
    try:
        from logger import info, warning, error, exception
        info("测试日志信息")
        warning("测试警告信息")
        error("测试错误信息")
        print(f"✓ 日志管理模块加载成功")
        print(f"  日志功能正常")
        return True
    except Exception as e:
        print(f"✗ 日志管理模块加载失败: {e}")
        return False


def test_cache_module():
    """测试数据缓存模块"""
    print("\n测试数据缓存模块...")
    try:
        from cache import cache_get, cache_set, cache_delete
        
        # 测试缓存设置和获取
        test_data = {'key': 'value', 'number': 42}
        cache_set('test_cache', test_data, 'test_param')
        cached_data = cache_get('test_cache', 'test_param', ttl=60)
        
        print(f"✓ 数据缓存模块加载成功")
        print(f"  缓存设置成功")
        print(f"  缓存获取成功: {cached_data}")
        
        # 测试缓存删除
        cache_delete('test_cache', 'test_param')
        deleted_data = cache_get('test_cache', 'test_param')
        print(f"  缓存删除成功: {deleted_data}")
        return True
    except Exception as e:
        print(f"✗ 数据缓存模块加载失败: {e}")
        return False


def test_user_config_module():
    """测试用户配置模块"""
    print("\n测试用户配置模块...")
    try:
        from user_config import get_user_config
        user_config = get_user_config()
        
        # 测试获取配置
        filter_conditions = user_config.get_filter_conditions()
        display_options = user_config.get_display_options()
        analysis_settings = user_config.get_analysis_settings()
        recent_stocks = user_config.get_recent_stocks()
        
        print(f"✓ 用户配置模块加载成功")
        print(f"  筛选条件: {filter_conditions}")
        print(f"  显示选项: {display_options}")
        print(f"  分析设置: {analysis_settings}")
        print(f"  最近股票: {recent_stocks}")
        
        # 测试保存配置
        user_config.set('test_key', 'test_value')
        test_value = user_config.get('test_key')
        print(f"  配置保存成功: {test_value}")
        return True
    except Exception as e:
        print(f"✗ 用户配置模块加载失败: {e}")
        return False


def test_chanlun_engine():
    """测试缠论引擎模块"""
    print("\n测试缠论引擎模块...")
    try:
        from chanlun_engine import ChanQuantEngine
        import pandas as pd
        import numpy as np
        from datetime import datetime, timedelta
        
        # 创建测试数据
        dates = pd.date_range(end=datetime.now(), periods=200, freq='D')
        prices = np.cumsum(np.random.randn(200) * 0.5) + 100
        
        test_df = pd.DataFrame({
            'open': prices * (1 - np.random.rand(200) * 0.01),
            'high': prices * (1 + np.random.rand(200) * 0.02),
            'low': prices * (1 - np.random.rand(200) * 0.02),
            'close': prices
        }, index=dates)
        
        # 测试缠论引擎
        engine = ChanQuantEngine(bi_threshold=0.03, use_macd=True)
        signals = engine.run(test_df)
        summary = engine.get_summary()
        bi_list = engine.get_bi_list()
        zhongshu_list = engine.get_zhongshu_list()
        
        print(f"✓ 缠论引擎模块加载成功")
        print(f"  分析摘要: {summary}")
        print(f"  笔数量: {len(bi_list)}")
        print(f"  中枢数量: {len(zhongshu_list)}")
        print(f"  信号数量: {len(signals)}")
        return True
    except Exception as e:
        print(f"✗ 缠论引擎模块加载失败: {e}")
        import traceback
        traceback.print_exc()
        return False


def test_data_source():
    """测试数据获取模块"""
    print("\n测试数据获取模块...")
    try:
        from data_source import EastMoneyData
        em = EastMoneyData()
        print(f"✓ 数据获取模块加载成功")
        print(f"  东方财富数据接口初始化成功")
        return True
    except Exception as e:
        print(f"✗ 数据获取模块加载失败: {e}")
        return False


def main():
    """主测试函数"""
    print("开始测试所有优化模块...")
    print("=" * 60)
    
    tests = [
        test_config_module,
        test_indicators_module,
        test_logger_module,
        test_cache_module,
        test_user_config_module,
        test_chanlun_engine,
        test_data_source
    ]
    
    passed = 0
    failed = 0
    
    for test in tests:
        if test():
            passed += 1
        else:
            failed += 1
    
    print("\n" + "=" * 60)
    print(f"测试完成: {passed} 通过, {failed} 失败")
    
    if failed == 0:
        print("✓ 所有模块测试通过，代码运行正常！")
    else:
        print("✗ 部分模块测试失败，请检查错误信息。")


if __name__ == "__main__":
    main()

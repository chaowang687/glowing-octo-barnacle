#!/usr/bin/env python3
# -*- coding: utf-8 -*-

from tencent_source import TencentDataSource

# 测试市盈率和市净率获取
source = TencentDataSource()

# 测试获取单只股票数据
df = source.get_realtime_quote(['600519'])
print("\n测试完成")
print(df[['代码', '名称', '最新价', '涨跌幅', '市盈率', '市净率']])

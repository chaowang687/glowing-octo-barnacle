#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试中文PDF生成
"""

from pdf_generator import PDFGenerator, generate_pdf_report

# 测试数据
stock_data = {
    'symbol': '000001',
    'name': '平安银行',
    'kline_data': None,
    'market_analysis': {
        'factors': {
            'bullish': ['公司业绩增长', '行业前景看好'],
            'bearish': ['市场竞争激烈'],
            'industry_hotspots': ['金融科技', '零售银行'],
            'market_trends': ['银行业整体稳健发展']
        },
        'main_funds': {
            'net_inflow': 10000000,
            'status': 'inflow'
        }
    }
}

analysis_result = "这是AI分析结果"

# 测试生成PDF
print("测试中文PDF生成...")
result = generate_pdf_report(stock_data, analysis_result, './test_reports')

if result:
    print(f"PDF生成成功: {result}")
    print("请检查PDF文件中的中文字符是否显示正常")
else:
    print("PDF生成失败")

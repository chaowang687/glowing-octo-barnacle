#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
资金流向可视化工具
获取近5日的主力/游资/散户资金数据，并绘制折线图
"""

import requests
import matplotlib.pyplot as plt
import matplotlib
import numpy as np
from datetime import datetime

# 设置中文字体
matplotlib.rcParams['font.sans-serif'] = ['SimHei', 'Arial Unicode MS', 'DejaVu Sans']
matplotlib.rcParams['axes.unicode_minus'] = False


def get_fund_flow_5days(stock_code: str) -> list:
    """
    获取近5日资金流向数据
    
    Args:
        stock_code: 股票代码
        
    Returns:
        资金数据列表
    """
    secid = f"0.{stock_code}" if not stock_code.startswith('6') else f"1.{stock_code}"
    
    url = 'http://push2his.eastmoney.com/api/qt/stock/fflow/daykline/get'
    params = {
        'lmt': 5,
        'klt': 101,
        'secid': secid,
        'fields1': 'f1,f2,f3,f7',
        'fields2': 'f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61,f62,f63,f64,f65',
        'ut': 'b2884a393a59ad64002292a3e90d46a5'
    }
    
    headers = {'User-Agent': 'Mozilla/5.0'}
    
    resp = requests.get(url, params=params, headers=headers, timeout=10)
    data = resp.json()
    
    klines = data.get('data', {}).get('klines', [])
    
    results = []
    for kline in klines:
        parts = kline.split(',')
        
        # 解析数据
        # 超大单 + 大单 = 主力资金
        # 中单 = 游资资金
        # 小单 = 散户资金
        
        super_large = float(parts[1])  # 超大单净流入
        large = float(parts[3])        # 大单净流入
        medium = float(parts[5])      # 中单净流入
        small = float(parts[7])        # 小单净流入
        
        # 计算各类资金
        main_force = super_large + large  # 主力 = 超大单 + 大单
        hot_money = medium               # 游资 = 中单
        retail = small                   # 散户 = 小单
        
        results.append({
            'date': parts[0],
            'super_large': super_large,
            'large': large,
            'medium': medium,
            'small': small,
            'main_force': main_force,  # 主力
            'hot_money': hot_money,    # 游资
            'retail': retail           # 散户
        })
    
    return results


def plot_fund_flow_chart(data: list, stock_name: str = "", output_file: str = None):
    """
    绘制资金流向折线图
    
    Args:
        data: 资金数据列表
        stock_name: 股票名称
        output_file: 输出文件路径
    """
    if not data:
        print("无数据可绘制")
        return
    
    # 提取数据
    dates = [d['date'][-5:] for d in data]  # 只显示月日
    main_force = [d['main_force'] / 10000 for d in data]  # 转换为万元
    hot_money = [d['hot_money'] / 10000 for d in data]
    retail = [d['retail'] / 10000 for d in data]
    
    # 创建图表
    fig, axes = plt.subplots(2, 1, figsize=(12, 10))
    
    # ==================== 子图1: 资金流向折线图 ====================
    ax1 = axes[0]
    
    x = np.arange(len(dates))
    width = 0.25
    
    # 绘制柱状图
    bars1 = ax1.bar(x - width, main_force, width, label='主力资金', color='#FF6B6B', alpha=0.8)
    bars2 = ax1.bar(x, hot_money, width, label='游资资金', color='#4ECDC4', alpha=0.8)
    bars3 = ax1.bar(x + width, retail, width, label='散户资金', color='#95A5A6', alpha=0.8)
    
    ax1.set_xlabel('日期', fontsize=12)
    ax1.set_ylabel('资金净流入 (万元)', fontsize=12)
    title = f'{stock_name} 近5日资金流向' if stock_name else '近5日资金流向'
    ax1.set_title(title, fontsize=14, fontweight='bold')
    ax1.set_xticks(x)
    ax1.set_xticklabels(dates)
    ax1.legend(loc='upper right')
    ax1.axhline(y=0, color='black', linestyle='-', linewidth=0.5)
    ax1.grid(axis='y', alpha=0.3)
    
    # 添加数值标签
    for bars in [bars1, bars2, bars3]:
        for bar in bars:
            height = bar.get_height()
            if height != 0:
                ax1.annotate(f'{height:.0f}万',
                            xy=(bar.get_x() + bar.get_width() / 2, height),
                            xytext=(0, 3),
                            textcoords="offset points",
                            ha='center', va='bottom', fontsize=8)
    
    # ==================== 子图2: 资金比例堆叠图 ====================
    ax2 = axes[1]
    
    # 计算每日各类资金的绝对值比例
    total_abs = []
    for i in range(len(data)):
        total = abs(main_force[i]) + abs(hot_money[i]) + abs(retail[i])
        total_abs.append(total if total > 0 else 1)  # 避免除零
    
    main_pct = [abs(m) / t * 100 for m, t in zip(main_force, total_abs)]
    hot_pct = [abs(h) / t * 100 for h, t in zip(hot_money, total_abs)]
    retail_pct = [abs(r) / t * 100 for r, t in zip(retail, total_abs)]
    
    # 绘制堆叠面积图
    ax2.fill_between(x, 0, main_pct, label='主力资金', color='#FF6B6B', alpha=0.7)
    ax2.fill_between(x, main_pct, [m + h for m, h in zip(main_pct, hot_pct)], 
                     label='游资资金', color='#4ECDC4', alpha=0.7)
    ax2.fill_between(x, [m + h for m, h in zip(main_pct, hot_pct)], 
                     [m + h + r for m, h, r in zip(main_pct, hot_pct, retail_pct)],
                     label='散户资金', color='#95A5A6', alpha=0.7)
    
    ax2.set_xlabel('日期', fontsize=12)
    ax2.set_ylabel('资金占比 (%)', fontsize=12)
    ax2.set_title('资金比例分布 (基于绝对值)', fontsize=14, fontweight='bold')
    ax2.set_xticks(x)
    ax2.set_xticklabels(dates)
    ax2.legend(loc='upper right')
    ax2.set_ylim(0, 100)
    ax2.grid(axis='y', alpha=0.3)
    
    plt.tight_layout()
    
    # 保存或显示
    if output_file:
        plt.savefig(output_file, dpi=150, bbox_inches='tight')
        print(f"图表已保存至: {output_file}")
    else:
        plt.show()


def print_fund_flow_table(data: list, stock_name: str = ""):
    """打印资金流向表格"""
    if not data:
        print("无数据")
        return
    
    print(f"\n{'='*70}")
    title = f" {stock_name} 近5日资金流向 " if stock_name else "近5日资金流向 "
    print(f"{title:^70}")
    print(f"{'='*70}")
    print(f"{'日期':<12} | {'主力资金':<15} | {'游资资金':<15} | {'散户资金':<15}")
    print(f"{'-'*70}")
    
    for d in data:
        main = d['main_force'] / 10000
        hot = d['hot_money'] / 10000
        retail = d['retail'] / 10000
        
        main_str = f"{main:,.0f}万"
        hot_str = f"{hot:,.0f}万"
        retail_str = f"{retail:,.0f}万"
        
        # 添加颜色标记
        if main >= 0:
            main_str = f"↑{main_str}"
        else:
            main_str = f"↓{main_str}"
            
        if hot >= 0:
            hot_str = f"↑{hot_str}"
        else:
            hot_str = f"↓{hot_str}"
            
        if retail >= 0:
            retail_str = f"↑{retail_str}"
        else:
            retail_str = f"↓{retail_str}"
        
        print(f"{d['date']:<12} | {main_str:<15} | {hot_str:<15} | {retail_str:<15}")
    
    print(f"{'='*70}")
    
    # 汇总
    total_main = sum(d['main_force'] for d in data) / 10000
    total_hot = sum(d['hot_money'] for d in data) / 10000
    total_retail = sum(d['retail'] for d in data) / 10000
    
    print(f"\n【5日合计】")
    print(f"  主力资金: {total_main:,.0f}万 ({'净流入' if total_main >= 0 else '净流出'})")
    print(f"  游资资金: {total_hot:,.0f}万 ({'净流入' if total_hot >= 0 else '净流出'})")
    print(f"  散户资金: {total_retail:,.0f}万 ({'净流入' if total_retail >= 0 else '净流出'})")


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description='资金流向可视化工具')
    parser.add_argument('-c', '--code', required=True, help='股票代码')
    parser.add_argument('-n', '--name', default='', help='股票名称')
    parser.add_argument('-o', '--output', default='', help='输出图片路径')
    
    args = parser.parse_args()
    
    print(f"正在获取 {args.code} 的资金流向数据...")
    
    data = get_fund_flow_5days(args.code)
    
    if not data:
        print("未能获取数据，请检查股票代码是否正确")
        return
    
    # 打印表格
    print_fund_flow_table(data, args.name)
    
    # 绘制图表
    output_file = args.output if args.output else None
    plot_fund_flow_chart(data, args.name, output_file)


if __name__ == '__main__':
    main()

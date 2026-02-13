#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
基本面筛选模块
功能：PE/PB筛选、ROE筛选、ST股过滤、净利润筛选
"""

import requests
import pandas as pd
import time
from typing import List, Dict, Optional
from datetime import datetime


class FundamentalSelector:
    """基本面选股器"""
    
    def __init__(self):
        self.headers = {
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
        }
        self.base_url = "http://push2.eastmoney.com"
    
    def get_stock_list_with_fundamental(self, count=100, sort_by='涨跌幅') -> pd.DataFrame:
        """
        获取股票列表（含基本面数据）
        
        Args:
            count: 获取数量
            sort_by: 排序字段 '涨跌幅' / '市盈率' / '总市值'
        
        Returns:
            DataFrame: 股票列表
        """
        # 排序字段映射
        sort_fields = {
            '涨跌幅': 'f3',      # 涨跌幅
            '市盈率': 'f162',    # 市盈率
            '总市值': 'f116',     # 总市值
            '成交额': 'f6',      # 成交额
        }
        fid = sort_fields.get(sort_by, 'f3')
        
        url = f"{self.base_url}/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': count,
            'po': 1 if sort_by == '涨跌幅' else 0,  # 升序/降序
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': fid,
            'fs': 'm:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23,m:0+t:81+s:2048',
            'fields': 'f2,f3,f4,f5,f6,f7,f8,f9,f10,f12,f13,f14,f15,f16,f17,f18,f20,f21,f23,f24,f25,f62,f116,f117,f128,f162,f163,f164,f167,f168,f169,f170,f171,f173,f177,f178,f184,f185,f186,f187,f188,f189,f190,f191,f192',
            '_': str(int(time.time() * 1000))
        }
        
        resp = requests.get(url, params=params, headers=self.headers, timeout=30)
        data = resp.json()
        
        stocks = []
        if 'data' in data and 'diff' in data['data']:
            for item in data['data']['diff']:
                stocks.append({
                    '代码': item.get('f12', ''),
                    '名称': item.get('f14', ''),
                    '最新价': item.get('f2', ''),
                    '涨跌幅': item.get('f3', ''),
                    '涨跌额': item.get('f4', ''),
                    '成交量': item.get('f5', ''),
                    '成交额': item.get('f6', ''),
                    '振幅': item.get('f7', ''),
                    '换手率': item.get('f8', ''),
                    '市盈率': item.get('f162', ''),          # PE
                    '市净率': item.get('f167', ''),          # PB
                    '总市值': item.get('f116', ''),          # 总市值(元)
                    '流通市值': item.get('f117', ''),        # 流通市值
                    '每股收益': item.get('f84', ''),         # 每股收益
                    '每股净资产': item.get('f85', ''),       # 每股净资产
                    '净资产收益率': item.get('f173', ''),    # ROE
                    '净利润同比增长': item.get('f191', ''),  # 净利润增长
                    '营收同比增长': item.get('f189', ''),    # 营收增长
                    '毛利率': item.get('f170', ''),         # 毛利率
                    '净利率': item.get('f171', ''),         # 净利率
                })
        
        df = pd.DataFrame(stocks)
        
        # 转换数值类型
        numeric_cols = ['最新价', '涨跌幅', '涨跌额', '成交量', '成交额', '振幅', '换手率',
                       '市盈率', '市净率', '总市值', '流通市值', '每股收益', '每股净资产',
                       '净资产收益率', '净利润同比增长', '营收同比增长', '毛利率', '净利率']
        
        for col in numeric_cols:
            if col in df.columns:
                df[col] = pd.to_numeric(df[col], errors='coerce')
        
        return df
    
    def filter_by_conditions(self, df: pd.DataFrame, 
                            min_price: float = 0,
                            max_price: float = float('inf'),
                            min_pe: float = None,        # 市盈率下限
                            max_pe: float = None,         # 市盈率上限
                            min_pb: float = None,        # 市净率下限
                            max_pb: float = None,        # 市净率上限
                            min_roe: float = None,      # 最小ROE
                            max_roe: float = None,      # 最大ROE
                            min_volume: float = None,    # 最小成交额(亿)
                            min_change: float = None,    # 最小涨跌幅
                            max_change: float = None,    # 最大涨跌幅
                            exclude_st: bool = True,     # 排除ST股
                            exclude_new: bool = True,    # 排除新股(上市不满60天)
                            industry: str = None,         # 行业筛选
                            ) -> pd.DataFrame:
        """
        基本面条件筛选
        
        Args:
            df: 股票DataFrame
            min_price: 最低股价
            max_price: 最高股价
            min_pe: 最低市盈率(负数表示亏损)
            max_pe: 最高市盈率
            min_pb: 最低市净率
            max_pb: 最高市净率
            min_roe: 最低净资产收益率(%)
            max_roe: 最高净资产收益率(%)
            min_volume: 最小成交额(亿元)
            min_change: 最小涨跌幅(%)
            max_change: 最大涨跌幅(%)
            exclude_st: 是否排除ST股
            exclude_new: 是否排除新股
            industry: 行业筛选
        
        Returns:
            DataFrame: 筛选后的股票
        """
        result = df.copy()
        
        # 价格筛选
        if min_price > 0:
            result = result[result['最新价'] >= min_price]
        if max_price < float('inf'):
            result = result[result['最新价'] <= max_price]
        
        # PE筛选
        if min_pe is not None:
            # 排除亏损股，只保留PE > 0的
            result = result[result['市盈率'] > 0]
            result = result[result['市盈率'] >= min_pe]
        if max_pe is not None:
            result = result[result['市盈率'] <= max_pe]
        
        # PB筛选
        if min_pb is not None:
            result = result[result['市净率'] > 0]
            result = result[result['市净率'] >= min_pb]
        if max_pb is not None:
            result = result[result['市净率'] <= max_pb]
        
        # ROE筛选
        if min_roe is not None:
            result = result[result['净资产收益率'] > min_roe]
        if max_roe is not None:
            result = result[result['净资产收益率'] < max_roe]
        
        # 成交额筛选
        if min_volume is not None:
            # 成交额单位是元，转换为亿
            result = result[result['成交额'] / 1e8 >= min_volume]
        
        # 涨跌幅筛选
        if min_change is not None:
            result = result[result['涨跌幅'] >= min_change]
        if max_change is not None:
            result = result[result['涨跌幅'] <= max_change]
        
        # 排除ST股
        if exclude_st:
            result = result[~result['名称'].str.contains('ST|退', na=False)]
        
        return result
    
    def get_value_stocks(self, count=50) -> pd.DataFrame:
        """
        获取低估值价值股
        
        筛选条件:
        - PE < 30 (放宽条件)
        - PB < 5
        - ROE > 3% (降低要求)
        - 成交额 > 5000万
        """
        df = self.get_stock_list_with_fundamental(count=count * 3)  # 多获取一些
        
        # 筛选条件 - 放宽一些
        filtered = df[
            (df['成交额'] > 5e7)  # 成交额 > 5000万
        ].copy()
        
        # 按ROE降序排列
        filtered = filtered.sort_values('净资产收益率', ascending=False)
        
        return filtered.head(count)
    
    def get_growth_stocks(self, count=50) -> pd.DataFrame:
        """
        获取高成长股
        
        筛选条件:
        - 净利润增长 > 10%
        - 营收增长 > 5%
        - ROE > 0%
        """
        df = self.get_stock_list_with_fundamental(count=count * 3)
        
        # 筛选条件 - 放宽
        filtered = df[
            (df['净利润同比增长'] > 10) &
            (df['营收同比增长'] > 5) &
            (df['净资产收益率'] > 0)
        ].copy()
        
        # 按净利润增长降序
        filtered = filtered.sort_values('净利润同比增长', ascending=False)
        
        return filtered.head(count)
    
    def get_dividend_stocks(self, count=50) -> pd.DataFrame:
        """
        获取高股息股
        
        筛选条件:
        - ROE > 10%
        - 市盈率 < 15
        - 成交额 > 5000万
        """
        df = self.get_stock_list_with_fundamental(count=count * 3)
        
        # 筛选条件
        filtered = df[
            (df['市盈率'] > 0) &
            (df['市盈率'] < 15) &
            (df['净资产收益率'] > 10) &
            (df['成交额'] > 5e7)  # 成交额 > 5000万
        ].copy()
        
        # 按ROE降序
        filtered = filtered.sort_values('净资产收益率', ascending=False)
        
        return filtered.head(count)
    
    def calculate_score(self, df: pd.DataFrame) -> pd.DataFrame:
        """
        计算综合评分
        
        评分维度:
        - 估值得分: PB越低越好
        - 盈利得分: ROE越高越好
        - 成长得分: 净利润增长越高越好
        - 流动性得分: 成交额越大越好
        """
        result = df.copy()
        
        # 1. 估值得分 (PB越低越高, 归一化到0-100)
        pb_scores = []
        for pb in result['市净率']:
            if pd.isna(pb) or pb <= 0:
                pb_scores.append(0)
            elif pb < 1:
                pb_scores.append(100)
            elif pb < 2:
                pb_scores.append(80)
            elif pb < 3:
                pb_scores.append(60)
            elif pb < 5:
                pb_scores.append(40)
            elif pb < 10:
                pb_scores.append(20)
            else:
                pb_scores.append(10)
        result['估值得分'] = pb_scores
        
        # 2. 盈利得分 (ROE越高越高)
        result['盈利得分'] = result['净资产收益率'].apply(
            lambda x: min(100, max(0, x * 5)) if pd.notna(x) else 0
        )
        
        # 3. 成长得分
        result['成长得分'] = result['净利润同比增长'].apply(
            lambda x: min(100, max(0, x)) if pd.notna(x) else 50
        )
        
        # 4. 流动性得分 (成交额归一化)
        max_vol = result['成交额'].max()
        if max_vol > 0:
            result['流动性得分'] = (result['成交额'] / max_vol * 100).fillna(0)
        else:
            result['流动性得分'] = 0
        
        # 5. 综合得分 (调整权重)
        result['综合得分'] = (
            result['估值得分'] * 0.20 +
            result['盈利得分'] * 0.30 +
            result['成长得分'] * 0.25 +
            result['流动性得分'] * 0.25
        ).round(1)
        
        # 按综合得分排序
        result = result.sort_values('综合得分', ascending=False)
        
        return result


# ================= 便捷函数 =================

def get_fundamental_stocks(count=100) -> pd.DataFrame:
    """获取含基本面数据的股票列表"""
    selector = FundamentalSelector()
    return selector.get_stock_list_with_fundamental(count)


def filter_value_stocks(min_pe=0, max_pe=20, min_roe=10) -> pd.DataFrame:
    """筛选价值股"""
    selector = FundamentalSelector()
    df = selector.get_stock_list_with_fundamental(200)
    return selector.filter_by_conditions(
        df, 
        max_pe=max_pe, 
        min_roe=min_roe,
        exclude_st=True
    )


def get_comprehensive_stocks(count=50) -> pd.DataFrame:
    """获取综合评分最高的股票"""
    selector = FundamentalSelector()
    df = selector.get_stock_list_with_fundamental(200)
    return selector.calculate_score(df).head(count)


# ================= 测试代码 =================

if __name__ == '__main__':
    selector = FundamentalSelector()
    
    print("=" * 60)
    print("基本面筛选系统测试")
    print("=" * 60)
    
    # 1. 获取股票列表
    print("\n📊 获取股票列表(含基本面)...")
    df = selector.get_stock_list_with_fundamental(50)
    print(f"获取到 {len(df)} 只股票")
    
    # 2. 条件筛选
    print("\n🔍 条件筛选 (PE<30, ROE>5%, 成交额>1亿)...")
    filtered = selector.filter_by_conditions(
        df,
        max_pe=30,
        min_roe=5,
        min_volume=1,
        exclude_st=True
    )
    print(f"筛选后: {len(filtered)} 只")
    
    if len(filtered) > 0:
        print("\n符合条件的股票:")
        for i, row in filtered.head(10).iterrows():
            print(f"  {row['代码']} {row['名称']:<8} "
                  f"价:{row['最新价']:>6.2f} PE:{row['市盈率']:>6.1f} "
                  f"ROE:{row['净资产收益率']:>5.1f}% 涨跌幅:{row['涨跌幅']:>5.1f}%")
    
    # 3. 价值股筛选
    print("\n📈 低估值价值股 (PE<20, PB<3, ROE>10%)...")
    value_stocks = selector.get_value_stocks(10)
    print(f"找到 {len(value_stocks)} 只")
    for i, row in value_stocks.iterrows():
        print(f"  {row['代码']} {row['名称']:<8} PE:{row['市盈率']:>5.1f} PB:{row['市净率']:>4.1f} ROE:{row['净资产收益率']:>5.1f}%")
    
    # 4. 综合评分
    print("\n🏆 综合评分TOP10...")
    df_full = selector.get_stock_list_with_fundamental(100)
    scored = selector.calculate_score(df_full)
    for i, row in scored.head(10).iterrows():
        print(f"  {row['代码']} {row['名称']:<8} "
              f"综合:{row['综合得分']:>5.1f} "
              f"估值:{row['估值得分']:>5.0f} "
              f"盈利:{row['盈利得分']:>5.0f} "
              f"成长:{row['成长得分']:>5.0f}")

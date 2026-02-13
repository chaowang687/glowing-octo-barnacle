#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
板块效应分析模块
功能：板块强度排名、领涨股识别、板块效应检测
"""

import pandas as pd
import numpy as np
from typing import List, Dict, Optional
from datetime import datetime, timedelta
import requests
import time


class SectorAnalysis:
    """板块效应分析引擎"""
    
    def __init__(self):
        self.headers = {
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36'
        }
        self.base_url = "http://push2.eastmoney.com"
        
        # 配置代理
        self.proxies = {
            'http': 'http://127.0.0.1:7890',
            'https': 'http://127.0.0.1:7890'
        }
        self.session = requests.Session()
        self.session.proxies = self.proxies
    
    def get_sector_list(self) -> pd.DataFrame:
        """获取板块列表"""
        url = f"{self.base_url}/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': 200,
            'po': 1,
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f3',
            'fs': 'm:90+t:2,m:90+t:23',
            'fields': 'f1,f2,f3,f4,f12,f13,f14',
            '_': str(int(time.time() * 1000))
        }
        
        resp = self.session.get(url, params=params, headers=self.headers, timeout=30)
        data = resp.json()
        
        sectors = []
        if 'data' in data and 'diff' in data['data']:
            for item in data['data']['diff']:
                sectors.append({
                    '板块代码': item.get('f12', ''),
                    '板块名称': item.get('f14', ''),
                    '涨跌幅': float(item.get('f3', 0) or 0),
                })
        
        return pd.DataFrame(sectors)
    
    def get_sector_stocks(self, sector_code: str) -> pd.DataFrame:
        """获取板块内个股"""
        url = f"{self.base_url}/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': 100,
            'po': 1,
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f3',
            'fs': f'b:{sector_code}',
            'fields': 'f1,f2,f3,f4,f5,f6,f12,f14',
            '_': str(int(time.time() * 1000))
        }
        
        resp = self.session.get(url, params=params, headers=self.headers, timeout=30)
        data = resp.json()
        
        stocks = []
        if 'data' in data and 'diff' in data['data']:
            for item in data['data']['diff']:
                stocks.append({
                    '代码': item.get('f12', ''),
                    '名称': item.get('f14', ''),
                    '最新价': float(item.get('f2', 0) or 0),
                    '涨跌幅': float(item.get('f3', 0) or 0),
                    '涨跌额': float(item.get('f4', 0) or 0),
                    '成交量': float(item.get('f5', 0) or 0),
                    '成交额': float(item.get('f6', 0) or 0),
                })
        
        return pd.DataFrame(stocks)
    
    def get_sector_kline(self, sector_code: str, days: int = 60) -> pd.DataFrame:
        """获取板块指数K线"""
        # 板块指数代码转换
        url = f"{self.base_url}/api/qt/stock/kline/get"
        
        # 东方财富板块指数代码格式
        secid = f"2.{sector_code}"
        
        end_date = datetime.now().strftime('%Y%m%d')
        start_date = (datetime.now() - timedelta(days=days*2)).strftime('%Y%m%d')
        
        params = {
            'secid': secid,
            'fields1': 'f1,f2,f3,f4,f5,f6',
            'fields2': 'f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61',
            'klt': 101,
            'fqt': 1,
            'beg': start_date,
            'end': end_date,
        }
        
        resp = self.session.get(url, params=params, headers=self.headers, timeout=30)
        data = resp.json()
        
        klines = []
        if 'data' in data and data['data'] and 'klines' in data['data']:
            for kline in data['data']['klines']:
                fields = kline.split(',')
                klines.append({
                    '日期': fields[0],
                    '开盘': float(fields[1]),
                    '收盘': float(fields[2]),
                    '最高': float(fields[3]),
                    '最低': float(fields[4]),
                    '成交量': float(fields[5]),
                    '成交额': float(fields[6]) if len(fields) > 6 else 0,
                })
        
        df = pd.DataFrame(klines)
        if len(df) > 0:
            df['日期'] = pd.to_datetime(df['日期'])
            df.set_index('日期', inplace=True)
        
        return df
    
    def calculate_sector_rps(self, sector_df: pd.DataFrame, period: int = 20) -> pd.DataFrame:
        """
        计算板块RPS（相对强度）
        
        Args:
            sector_df: 板块列表DataFrame
            period: 计算周期
        
        Returns:
            添加了RPS值的板块DataFrame
        """
        if len(sector_df) == 0:
            return sector_df
        
        # 根据涨跌幅排序，计算RPS
        sector_df = sector_df.sort_values('涨跌幅', ascending=False).reset_index(drop=True)
        sector_df['排名'] = range(1, len(sector_df) + 1)
        
        # RPS = (n - rank + 1) / n * 100
        n = len(sector_df)
        sector_df['RPS'] = (n - sector_df['排名'] + 1) / n * 100
        
        return sector_df
    
    def get_sector_strength(self, top_n: int = 20) -> pd.DataFrame:
        """
        获取强势板块
        
        Args:
            top_n: 返回前N个强势板块
        
        Returns:
            强势板块列表
        """
        sectors = self.get_sector_list()
        
        if len(sectors) == 0:
            return pd.DataFrame()
        
        # 计算RPS
        sectors = self.calculate_sector_rps(sectors)
        
        # 按涨跌幅排序，取前N个
        sectors = sectors.sort_values('涨跌幅', ascending=False).head(top_n)
        
        return sectors
    
    def get_weak_sectors(self, top_n: int = 20) -> pd.DataFrame:
        """获取弱势板块"""
        sectors = self.get_sector_list()
        
        if len(sectors) == 0:
            return pd.DataFrame()
        
        sectors = self.calculate_sector_rps(sectors)
        sectors = sectors.sort_values('涨跌幅', ascending=True).head(top_n)
        
        return sectors
    
    def analyze_sector(self, sector_code: str) -> Dict:
        """
        全面分析单个板块
        
        Args:
            sector_code: 板块代码
        
        Returns:
            板块分析结果
        """
        # 获取板块基本信息
        sectors = self.get_sector_list()
        sector_info = sectors[sectors['板块代码'] == sector_code]
        
        if len(sector_info) == 0:
            return {}
        
        sector_name = sector_info.iloc[0]['板块名称']
        change_pct = sector_info.iloc[0]['涨跌幅']
        
        # 获取板块内个股
        stocks = self.get_sector_stocks(sector_code)
        
        # 找出领涨股（涨幅最大的3只）
        leaders = []
        if len(stocks) > 0:
            stocks_sorted = stocks.sort_values('涨跌幅', ascending=False).head(5)
            for _, row in stocks_sorted.iterrows():
                leaders.append({
                    '代码': row['代码'],
                    '名称': row['名称'],
                    '涨跌幅': row['涨跌幅'],
                    '最新价': row['最新价'],
                })
            
            # 计算板块平均涨幅
            avg_change = stocks['涨跌幅'].mean()
            # 计算上涨个股比例
            up_ratio = (stocks['涨跌幅'] > 0).sum() / len(stocks) * 100 if len(stocks) > 0 else 0
        else:
            avg_change = 0
            up_ratio = 0
        
        return {
            '板块代码': sector_code,
            '板块名称': sector_name,
            '涨跌幅': change_pct,
            '平均涨跌幅': round(avg_change, 2),
            '上涨比例': round(up_ratio, 2),
            '个股数量': len(stocks),
            '领涨股': leaders,
        }
    
    def get_sector_effect_stocks(self, min_strength: float = 70, 
                                   min_leader_change: float = 5.0,
                                   top_sectors: int = 10) -> List[Dict]:
        """
        获取板块效应下的领涨股
        
        条件：
        1. 板块RPS >= min_strength（强势板块）
        2. 领涨股涨幅 >= min_leader_change
        
        Args:
            min_strength: 最小板块RPS强度
            min_leader_change: 领涨股最小涨幅
            top_sectors: 分析前N个强势板块
        
        Returns:
            符合条件的领涨股列表
        """
        # 获取强势板块
        strong_sectors = self.get_sector_strength(top_n=top_sectors)
        
        effect_stocks = []
        
        for _, sector in strong_sectors.iterrows():
            sector_code = sector['板块代码']
            sector_name = sector['板块名称']
            rps = sector['RPS']
            change = sector['涨跌幅']
            
            # 只分析RPS >= 70的板块
            if rps < min_strength:
                continue
            
            # 获取板块内个股
            stocks = self.get_sector_stocks(sector_code)
            
            if len(stocks) == 0:
                continue
            
            # 按涨幅排序
            stocks = stocks.sort_values('涨跌幅', ascending=False)
            
            # 取涨幅最大的个股作为领涨股
            top_stock = stocks.iloc[0]
            
            # 检查领涨股涨幅是否满足条件
            if top_stock['涨跌幅'] >= min_leader_change:
                effect_stocks.append({
                    '股票代码': top_stock['代码'],
                    '股票名称': top_stock['名称'],
                    '所属板块': sector_name,
                    '板块代码': sector_code,
                    '板块涨跌幅': change,
                    '板块RPS': round(rps, 2),
                    '个股涨跌幅': top_stock['涨跌幅'],
                    '最新价': top_stock['最新价'],
                    '板块内排名': 1,
                    '效应强度': '强' if rps >= 85 else '中' if rps >= 70 else '弱',
                })
                
                # 如果有第二、第三领涨股也满足条件，也加入
                for i in range(1, min(3, len(stocks))):
                    stock = stocks.iloc[i]
                    if stock['涨跌幅'] >= min_leader_change:
                        effect_stocks.append({
                            '股票代码': stock['代码'],
                            '股票名称': stock['名称'],
                            '所属板块': sector_name,
                            '板块代码': sector_code,
                            '板块涨跌幅': change,
                            '板块RPS': round(rps, 2),
                            '个股涨跌幅': stock['涨跌幅'],
                            '最新价': stock['最新价'],
                            '板块内排名': i + 1,
                            '效应强度': '强' if rps >= 85 else '中' if rps >= 70 else '弱',
                        })
        
        return effect_stocks
    
    def get_market_context(self) -> Dict:
        """
        获取市场整体状态
        
        Returns:
            市场状态字典
        """
        # 获取主要指数
        url = f"{self.base_url}/api/qt/ulist.np/get"
        indices = {
            '1.000001': '上证指数',
            '0.399001': '深证成指',
            '0.399006': '创业板指',
            '0.000300': '沪深300',
            '1.000016': '上证50',
        }
        
        params = {
            'fltt': 2,
            'invt': 2,
            'fields': 'f1,f2,f3,f4,f12,f13,f14',
            'secids': ','.join(indices.keys()),
            '_': str(int(time.time() * 1000))
        }
        
        resp = requests.get(url, params=params, headers=self.headers, timeout=10)
        data = resp.json()
        
        index_data = []
        if 'data' in data and 'diff' in data['data']:
            for item in data['data']['diff']:
                index_data.append({
                    '代码': item.get('f12', ''),
                    '名称': item.get('f14', ''),
                    '最新价': float(item.get('f2', 0) or 0),
                    '涨跌幅': float(item.get('f3', 0) or 0),
                })
        
        # 获取涨跌幅前50
        url2 = f"{self.base_url}/api/qt/clist/get"
        params2 = {
            'pn': 1,
            'pz': 50,
            'po': 1,
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f3',
            'fs': 'm:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23',
            'fields': 'f1,f2,f3,f4,f12,f14',
            '_': str(int(time.time() * 1000))
        }
        
        resp2 = requests.get(url2, params=params2, headers=self.headers, timeout=10)
        data2 = resp2.json()
        
        up_count = 0
        down_count = 0
        if 'data' in data2 and 'diff' in data2['data']:
            for item in data2['data']['diff']:
                change = float(item.get('f3', 0) or 0)
                if change > 0:
                    up_count += 1
                elif change < 0:
                    down_count += 1
        
        # 计算市场情绪
        total = up_count + down_count
        up_ratio = up_count / total * 100 if total > 0 else 50
        
        if up_ratio >= 70:
            sentiment = '强势'
        elif up_ratio >= 55:
            sentiment = '偏强'
        elif up_ratio >= 45:
            sentiment = '中性'
        elif up_ratio >= 30:
            sentiment = '偏弱'
        else:
            sentiment = '弱势'
        
        # 计算市场RPS（类似个股RPS）
        market_rps = (up_count - down_count + total) / (2 * total) * 100 if total > 0 else 50
        
        return {
            '指数行情': pd.DataFrame(index_data),
            '上涨家数': up_count,
            '下跌家数': down_count,
            '上涨比例': round(up_ratio, 2),
            '市场情绪': sentiment,
            '市场RPS': round(market_rps, 2),
            '数据时间': datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
        }


class SectorSelector:
    """板块效应选股器 - 将板块效应与选股系统集成"""
    
    def __init__(self):
        self.sector_analysis = SectorAnalysis()
    
    def select_by_sector_effect(self, 
                                  min_sector_rps: float = 70,
                                  min_stock_change: float = 3.0,
                                  top_sectors: int = 15) -> pd.DataFrame:
        """
        基于板块效应选股
        
        选出处于强势板块中且涨幅较大的股票
        
        Args:
            min_sector_rps: 最小板块RPS
            min_stock_change: 最小个股涨幅
            top_sectors: 分析的前N个强势板块
        
        Returns:
            符合条件的股票DataFrame
        """
        # 获取板块效应下的领涨股
        effect_stocks = self.sector_analysis.get_sector_effect_stocks(
            min_strength=min_sector_rps,
            min_leader_change=min_stock_change,
            top_sectors=top_sectors
        )
        
        if not effect_stocks:
            return pd.DataFrame()
        
        df = pd.DataFrame(effect_stocks)
        
        # 添加选股得分
        df['选股得分'] = df['板块RPS'] * 0.4 + df['个股涨跌幅'] * 2
        df = df.sort_values('选股得分', ascending=False)
        
        return df
    
    def get_sector_leaders(self, sector_name: str = None, 
                           sector_code: str = None) -> pd.DataFrame:
        """
        获取板块内领涨股
        
        Args:
            sector_name: 板块名称
            sector_code: 板块代码（优先）
        
        Returns:
            领涨股列表
        """
        if sector_code is None and sector_name:
            # 通过名称查找代码
            sectors = self.sector_analysis.get_sector_list()
            match = sectors[sectors['板块名称'].str.contains(sector_name)]
            if len(match) > 0:
                sector_code = match.iloc[0]['板块代码']
        
        if sector_code is None:
            return pd.DataFrame()
        
        stocks = self.sector_analysis.get_sector_stocks(sector_code)
        
        if len(stocks) > 0:
            stocks = stocks.sort_values('涨跌幅', ascending=False)
            stocks['板块内排名'] = range(1, len(stocks) + 1)
        
        return stocks
    
    def comprehensive_sector_analysis(self) -> Dict:
        """
        综合板块分析报告
        
        Returns:
            包含市场状态、强势板块、效应个股的完整报告
        """
        # 市场整体状态
        market = self.sector_analysis.get_market_context()
        
        # 强势板块
        strong_sectors = self.sector_analysis.get_sector_strength(top_n=15)
        
        # 板块效应领涨股
        effect_stocks = self.sector_analysis.get_sector_effect_stocks(
            min_strength=70,
            min_leader_change=5.0,
            top_sectors=15
        )
        
        effect_df = pd.DataFrame(effect_stocks) if effect_stocks else pd.DataFrame()
        
        return {
            '市场状态': market,
            '强势板块': strong_sectors,
            '效应个股': effect_df,
        }


# ================= 便捷函数 =================

def get_sector_strength(n: int = 20) -> pd.DataFrame:
    """获取强势板块"""
    sa = SectorAnalysis()
    return sa.get_sector_strength(n)


def get_effect_stocks(min_rps: float = 70, min_change: float = 5.0) -> List[Dict]:
    """获取板块效应领涨股"""
    sa = SectorAnalysis()
    return sa.get_sector_effect_stocks(min_strength=min_rps, min_leader_change=min_change)


def sector_select(min_rps: float = 70, min_change: float = 3.0) -> pd.DataFrame:
    """基于板块效应选股"""
    selector = SectorSelector()
    return selector.select_by_sector_effect(min_sector_rps=min_rps, min_stock_change=min_change)


# ================= 测试代码 =================
if __name__ == '__main__':
    sa = SectorAnalysis()
    
    print("=" * 60)
    print("板块效应分析测试")
    print("=" * 60)
    
    # 1. 市场状态
    print("\n📊 市场整体状态:")
    market = sa.get_market_context()
    print(f"  市场情绪: {market['市场情绪']}")
    print(f"  上涨比例: {market['上涨比例']}%")
    print(f"  市场RPS: {market['市场RPS']}")
    print("\n主要指数:")
    for _, idx in market['指数行情'].iterrows():
        print(f"  {idx['名称']:<10} {idx['最新价']:>8.2f} {idx['涨跌幅']:>7.2f}%")
    
    # 2. 强势板块
    print("\n🔥 强势板块TOP10:")
    strong = sa.get_sector_strength(10)
    for _, s in strong.iterrows():
        print(f"  {s['板块名称']:<12} {s['涨跌幅']:>7.2f}%  RPS:{s['RPS']:>5.1f}")
    
    # 3. 板块效应领涨股
    print("\n🚀 板块效应领涨股:")
    effect = sa.get_sector_effect_stocks(min_strength=70, min_leader_change=5.0)
    for e in effect[:10]:
        print(f"  {e['股票代码']} {e['股票名称']:<8} {e['所属板块']:<10} 涨{e['个股涨跌幅']:>6.2f}%  效应:{e['效应强度']}")
    
    # 4. 综合选股
    print("\n💰 基于板块效应选股:")
    selector = SectorSelector()
    df = selector.select_by_sector_effect(min_sector_rps=70, min_stock_change=3.0)
    if len(df) > 0:
        for _, row in df.head(10).iterrows():
            print(f"  {row['股票代码']} {row['股票名称']:<8} 板块:{row['所属板块']:<8} 得分:{row['选股得分']:.1f}")

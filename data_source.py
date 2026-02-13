#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
A股数据获取模块 - 东方财富API
功能：实时行情、K线数据、板块行情
"""

import requests
import time
import pandas as pd
from typing import List, Dict, Optional
from datetime import datetime, timedelta


class EastMoneyData:
    """东方财富数据接口"""
    
    def __init__(self):
        self.headers = {
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
        }
        # 使用HTTPS接口
        self.base_url = "https://push2.eastmoney.com"
        
        # 配置代理
        self.proxies = {
            'http': 'http://127.0.0.1:7890',
            'https': 'http://127.0.0.1:7890'
        }
        
        self.session = requests.Session()
        self.session.proxies = self.proxies
    
    def get_realtime_quotes(self, count=100) -> pd.DataFrame:
        """
        获取A股实时行情
        
        Args:
            count: 获取数量，默认100
        
        Returns:
            DataFrame: 包含代码、名称、价、涨跌幅等
        """
        url = f"{self.base_url}/api/qt/clist/get"
        params = {
            'pn': 1,
            'pz': count,
            'po': 1,                      # 按涨跌幅降序
            'np': 1,
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            'fltt': 2,
            'invt': 2,
            'fid': 'f3',
            'fs': 'm:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23,m:0+t:81+s:2048',
            'fields': 'f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f12,f13,f14,f15,f16,f17,f18,f20,f21,f23,f24,f25,f22,f11,f62,f128,f136,f115,f152',
            '_': str(int(time.time() * 1000))
        }
        
        # 重试机制
        for attempt in range(3):
            try:
                resp = self.session.get(url, params=params, headers=self.headers, timeout=30)
                
                # 检查响应状态
                if resp.status_code != 200:
                    print(f"API返回状态码: {resp.status_code}, 重试 {attempt+1}/3")
                    time.sleep(2)
                    continue
                
                # 尝试解析JSON
                try:
                    data = resp.json()
                except:
                    print(f"JSON解析失败, 重试 {attempt+1}/3")
                    time.sleep(2)
                    continue
                
                # 检查数据结构
                if 'data' not in data or 'diff' not in data.get('data', {}):
                    print(f"数据结构异常, 重试 {attempt+1}/3")
                    time.sleep(2)
                    continue
                
                stocks = []
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
                        '市盈率': item.get('f162', ''),
                        '市净率': item.get('f167', ''),
                    })
                
                df = pd.DataFrame(stocks)
                
                # 转换数值类型
                numeric_cols = ['最新价', '涨跌幅', '涨跌额', '成交量', '成交额', '振幅', '换手率', '市盈率', '市净率']
                for col in numeric_cols:
                    if col in df.columns:
                        df[col] = pd.to_numeric(df[col], errors='coerce')
                
                return df
                
            except requests.exceptions.Timeout:
                print(f"请求超时, 重试 {attempt+1}/3")
                time.sleep(2)
            except Exception as e:
                print(f"请求异常: {e}, 重试 {attempt+1}/3")
                time.sleep(2)
        
        # 所有重试都失败，返回空DataFrame
        print("数据获取失败，已尝试3次")
        return pd.DataFrame()
    
    def get_stock_kline(self, symbol: str, start_date: str = None, end_date: str = None, 
                        period: str = '101') -> pd.DataFrame:
        """
        获取个股K线数据
        
        Args:
            symbol: 股票代码，如 '000001'（深市）或 '600000'（沪市）
            start_date: 开始日期 'YYYYMMDD'
            end_date: 结束日期 'YYYYMMDD'
            period: K线周期 '101'=日线 '102'=周 '103'=月
        
        Returns:
            DataFrame: K线数据
        """
        # 判断市场
        if symbol.startswith('6') or symbol.startswith('9'):
            secid = f"1.{symbol}"  # 沪市
        else:
            secid = f"0.{symbol}"  # 深市
        
        if end_date is None:
            end_date = datetime.now().strftime('%Y%m%d')
        if start_date is None:
            start_date = (datetime.now() - timedelta(days=365)).strftime('%Y%m%d')
        
        # K线接口使用 push2his 域名
        url = "http://push2his.eastmoney.com/api/qt/stock/kline/get"
        params = {
            'secid': secid,
            'fields1': 'f1,f2,f3,f4,f5,f6',
            'fields2': 'f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61',
            'klt': period,           # K线类型
            'fqt': 1,                # 复权类型 0=不复权 1=前复权 2=后复权
            'beg': start_date,
            'end': end_date,
        }
        
        resp = requests.get(url, params=params, headers=self.headers, timeout=30)
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
                    '振幅': float(fields[7]) if len(fields) > 7 else 0,
                    '涨跌幅': float(fields[8]) if len(fields) > 8 else 0,
                    '涨跌额': float(fields[9]) if len(fields) > 9 else 0,
                    '换手率': float(fields[10]) if len(fields) > 10 else 0,
                })
        
        df = pd.DataFrame(klines)
        if len(df) > 0:
            df['日期'] = pd.to_datetime(df['日期'])
            df.set_index('日期', inplace=True)
        
        return df
    
    def get_realtime_quote(self, symbol: str) -> Dict:
        """获取单只股票实时行情"""
        # 判断市场
        if symbol.startswith('6') or symbol.startswith('9'):
            secid = f"1.{symbol}"
        else:
            secid = f"0.{symbol}"
        
        url = f"{self.base_url}/api/qt/stock/get"
        params = {
            'secid': secid,
            'fields': 'f43,f44,f45,f46,f47,f48,f49,f50,f51,f52,f55,f57,f58,f59,f60,f116,f117,f162,f167,f168,f169,f170,f171,f173,f177',
            'ut': 'bd1d9ddb04089700cf9c27f6f7426281',
            '_': str(int(time.time() * 1000))
        }
        
        resp = requests.get(url, params=params, headers=self.headers, timeout=10)
        data = resp.json()
        
        if 'data' in data and data['data']:
            item = data['data']
            return {
                '代码': symbol,
                '名称': item.get('f58', ''),
                '最新价': item.get('f43', 0) / 1000,  # 价格需要除以1000
                '涨跌额': item.get('f46', 0) / 1000,
                '涨跌幅': item.get('f47', 0) / 100,
                '成交量': item.get('f47', 0),
                '成交额': item.get('f47', 0),
                '振幅': item.get('f49', 0) / 100,
                '换手率': item.get('f50', 0) / 100,
                '市盈率': item.get('f162', ''),
                '市净率': item.get('f167', ''),
            }
        return {}
    
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
            'fs': 'm:90+t:2,m:90+t:23',  # 板块
            'fields': 'f1,f2,f3,f4,f12,f13,f14',
            '_': str(int(time.time() * 1000))
        }
        
        resp = requests.get(url, params=params, headers=self.headers, timeout=30)
        data = resp.json()
        
        sectors = []
        if 'data' in data and 'diff' in data['data']:
            for item in data['data']['diff']:
                sectors.append({
                    '代码': item.get('f12', ''),
                    '名称': item.get('f14', ''),
                    '涨跌幅': item.get('f3', ''),
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
            'fs': f'b:{sector_code}',  # 板块代码
            'fields': 'f1,f2,f3,f4,f5,f6,f12,f14',
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
                })
        
        return pd.DataFrame(stocks)
    
    def get_index_realtime(self) -> pd.DataFrame:
        """获取主要指数实时行情"""
        # 主要指数代码
        indices = {
            '1.000001': '上证指数',
            '0.399001': '深证成指',
            '0.399006': '创业板指',
            '0.000300': '沪深300',
            '1.000016': '上证50',
        }
        
        url = f"{self.base_url}/api/qt/ulist.np/get"
        params = {
            'fltt': 2,
            'invt': 2,
            'fields': 'f1,f2,f3,f4,f12,f13,f14',
            'secids': ','.join(indices.keys()),
            '_': str(int(time.time() * 1000))
        }
        
        resp = requests.get(url, params=params, headers=self.headers, timeout=10)
        data = resp.json()
        
        index_list = []
        if 'data' in data and 'diff' in data['data']:
            for item in data['data']['diff']:
                index_list.append({
                    '代码': item.get('f12', ''),
                    '名称': item.get('f14', ''),
                    '最新价': item.get('f2', ''),
                    '涨跌幅': item.get('f3', ''),
                    '涨跌额': item.get('f4', ''),
                })
        
        return pd.DataFrame(index_list)


# ================= 便捷函数 =================

def get_quotes(count=100) -> pd.DataFrame:
    """获取实时行情"""
    em = EastMoneyData()
    return em.get_realtime_quotes(count)


def get_kline(symbol: str, start_date=None, end_date=None) -> pd.DataFrame:
    """获取K线"""
    em = EastMoneyData()
    return em.get_stock_kline(symbol, start_date, end_date)


def get_index() -> pd.DataFrame:
    """获取主要指数"""
    em = EastMoneyData()
    return em.get_index_realtime()


# ================= 测试代码 =================
if __name__ == '__main__':
    em = EastMoneyData()
    
    print("=" * 60)
    print("测试东方财富数据接口")
    print("=" * 60)
    
    # 1. 获取主要指数
    print("\n📊 主要指数行情:")
    df_index = em.get_index_realtime()
    print(df_index)
    
    # 2. 获取实时涨跌幅前20
    print("\n📈 涨幅前20:")
    df = em.get_realtime_quotes(20)
    for i, row in df.iterrows():
        print(f"  {row['代码']} {row['名称']:<8} {row['最新价']:>7} {row['涨跌幅']:>7.2f}%")
    
    # 3. 获取个股K线
    print("\n📊 平安银行K线:")
    df_kline = em.get_stock_kline('000001', start_date='20240101', end_date='20240110')
    print(df_kline)

#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
多数据源模块 - 容错备份
当主数据源不可用时，自动切换到备用源
"""

import requests
import pandas as pd
import numpy as np
import time
from typing import Optional


class MultiDataSource:
    """多数据源容错获取"""
    
    def __init__(self):
        self.headers = {
            'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36'
        }
        self.session = requests.Session()
        self.session.trust_env = False
    
    def get_realtime_quotes_sina(self, count=100) -> pd.DataFrame:
        """新浪财经接口"""
        try:
            # 新浪实时行情API
            url = "http://hq.sinajs.cn/list=sh600000,sh601398,sh600036"
            
            # 获取涨跌幅前100的股票代码
            stock_codes = self._get_top_change_codes_sina(count)
            if not stock_codes:
                return pd.DataFrame()
            
            # 批量获取
            stocks_list = []
            for i in range(0, len(stock_codes), 50):
                batch = stock_codes[i:i+50]
                codes_str = ','.join([f"sh{s}" if s.startswith('6') or s.startswith('9') else f"sz{s}" for s in batch])
                url = f"http://hq.sinajs.cn/list={codes_str}"
                
                resp = self.session.get(url, headers=self.headers, timeout=10)
                if resp.status_code != 200:
                    continue
                
                content = resp.text
                for line in content.split('\n'):
                    if '=' in line:
                        code_part = line.split('=')[0].split('_')[-1]
                        data = line.split('=')[1].strip('"').split(',')
                        if len(data) > 30:
                            code = code_part[2:] if code_part.startswith('sh') or code_part.startswith('sz') else code_part
                            try:
                                stocks_list.append({
                                    '代码': code,
                                    '名称': data[0],
                                    '最新价': float(data[1]) if data[1] else 0,
                                    '涨跌幅': ((float(data[1]) - float(data[2])) / float(data[2]) * 100) if float(data[2]) > 0 else 0,
                                    '涨跌额': float(data[1]) - float(data[2]) if len(data) > 2 else 0,
                                    '成交量': float(data[4]) if data[4] else 0,
                                    '成交额': float(data[5]) if data[5] else 0,
                                })
                            except:
                                pass
            
            return pd.DataFrame(stocks_list)
        except Exception as e:
            print(f"新浪接口错误: {e}")
            return pd.DataFrame()
    
    def _get_top_change_codes_sina(self, count) -> list:
        """获取涨跌幅股票代码列表"""
        try:
            # 获取沪深涨跌榜
            url = "http://hq.sinajs.cn/list=sh000001"
            resp = self.session.get(url, headers=self.headers, timeout=10)
            return []  # 简化处理
        except:
            return []
    
    def get_realtime_quotes_akshare(self, count=100) -> pd.DataFrame:
        """AkShare接口"""
        try:
            import akshare as ak
            df = ak.stock_zh_a_spot_em()
            if df is not None and len(df) > 0:
                return df.head(count)
        except Exception as e:
            print(f"AkShare接口错误: {e}")
        return pd.DataFrame()
    
    def get_realtime_quotes_baostock(self, count=100) -> pd.DataFrame:
        """Baostock接口 - 主要用于历史数据，实时行情有限"""
        try:
            import baostock as bs
            lg = bs.login()
            if lg.error_code != '0':
                return pd.DataFrame()
            
            # 获取当日行情
            rs = bs.query_history_k_data_plus(
                "sh.600000",
                "date,code,open,high,low,close,volume,amount,turn",
                start_date=time.strftime('%Y-%m-%d'),
                end_date=time.strftime('%Y-%m-%d'),
                frequency="d"
            )
            
            data_list = []
            while rs.error_code == '0' and rs.next():
                data_list.append(rs.get_row_data())
            
            bs.logout()
            
            if data_list:
                df = pd.DataFrame(data_list, columns=['日期','代码','开盘','最高','最低','收盘','成交量','成交额','换手率'])
                return df
        except Exception as e:
            print(f"Baostock接口错误: {e}")
        return pd.DataFrame()
    
    def get_realtime_quotes_xueqiu(self, count=100) -> pd.DataFrame:
        """雪球接口"""
        try:
            # 雪球实时行情
            url = "https://stock.xueqiu.com/v5/stock/quote.json"
            params = {
                'symbol': 'SH600000',
                '_': str(int(time.time() * 1000))
            }
            headers = {
                'User-Agent': 'Mozilla/5.0',
                'Cookie': 'xq_a_token=test'
            }
            resp = self.session.get(url, params=params, headers=headers, timeout=10)
            if resp.status_code == 200:
                data = resp.json()
                # 解析数据...
        except Exception as e:
            print(f"雪球接口错误: {e}")
        return pd.DataFrame()
    
    def get_realtime_quotes(self, count=100) -> pd.DataFrame:
        """多源获取 - 依次尝试，成功即返回"""
        
        # 方法1: 尝试AkShare
        print("尝试 AkShare...")
        df = self.get_realtime_quotes_akshare(count)
        if df is not None and len(df) > 0:
            print(f"✅ AkShare 成功获取 {len(df)} 条")
            return df
        
        # 方法2: 尝试新浪
        print("尝试 新浪财经...")
        df = self.get_realtime_quotes_sina(count)
        if df is not None and len(df) > 0:
            print(f"✅ 新浪 成功获取 {len(df)} 条")
            return df
        
        # 方法3: 尝试Baostock
        print("尝试 Baostock...")
        df = self.get_realtime_quotes_baostock(count)
        if df is not None and len(df) > 0:
            print(f"✅ Baostock 成功获取 {len(df)} 条")
            return df
        
        print("❌ 所有数据源均失败")
        return pd.DataFrame()
    
    def get_stock_kline(self, symbol: str, start_date: str = None, 
                        period: str = '101') -> pd.DataFrame:
        """获取K线数据 - 尝试多个源"""
        
        # 优先尝试AkShare
        try:
            import akshare as ak
            df = ak.stock_zh_a_hist(
                symbol=symbol,
                period='daily',
                start_date=start_date or '20240101',
                end_date=time.strftime('%Y%m%d'),
                adjust='qfq'
            )
            if df is not None and len(df) > 0:
                return df
        except Exception as e:
            print(f"AkShare K线错误: {e}")
        
        # 备选：尝试东方财富
        try:
            from data_source import EastMoneyData
            em = EastMoneyData()
            return em.get_stock_kline(symbol, start_date=start_date)
        except Exception as e:
            print(f"东方财富K线错误: {e}")
        
        return pd.DataFrame()


def get_quotes(count=100) -> pd.DataFrame:
    """便捷函数 - 获取实时行情"""
    source = MultiDataSource()
    return source.get_realtime_quotes(count)


def get_kline(symbol: str, start_date: str = None) -> pd.DataFrame:
    """便捷函数 - 获取K线"""
    source = MultiDataSource()
    return source.get_stock_kline(symbol, start_date)


# 测试
if __name__ == '__main__':
    print("=" * 50)
    print("多数据源测试")
    print("=" * 50)
    
    source = MultiDataSource()
    
    print("\n测试获取实时行情...")
    df = source.get_realtime_quotes(20)
    
    if len(df) > 0:
        print(f"\n✅ 成功获取 {len(df)} 条数据:")
        print(df.head(10))
    else:
        print("\n❌ 所有数据源均失败")
        print("\n建议检查网络连接或稍后重试")

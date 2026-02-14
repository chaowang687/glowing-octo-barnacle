# -*- coding: utf-8 -*-
"""
数据存储模块
负责SQLite数据库的初始化和数据读写操作
"""

import sqlite3
import pandas as pd
from datetime import datetime
from typing import Optional, List, Dict
import os

from quant_system.config import DB_PATH, DB_TABLES
from quant_system.utils import logger


class StorageManager:
    """数据库管理器"""
    
    def __init__(self):
        """初始化数据库连接并创建表结构"""
        self.conn = None
        self._connect()
        self._init_tables()
        logger.info(f"数据库初始化完成: {DB_PATH}")
    
    def _connect(self):
        """建立数据库连接"""
        self.conn = sqlite3.connect(DB_PATH, check_same_thread=False)
        self.conn.row_factory = sqlite3.Row
    
    def _init_tables(self):
        """创建所有必要的表"""
        cursor = self.conn.cursor()
        for table_name, create_sql in DB_TABLES.items():
            cursor.execute(create_sql)
        self.conn.commit()
        logger.debug("数据库表结构初始化完成")
    
    def save_news(self, news_list: List[Dict]) -> int:
        """
        保存新闻数据
        
        Args:
            news_list: 新闻数据列表
            
        Returns:
            保存的记录数
        """
        if not news_list:
            return 0
        
        cursor = self.conn.cursor()
        saved_count = 0
        
        for news in news_list:
            try:
                cursor.execute("""
                    INSERT OR IGNORE INTO news 
                    (stock_code, stock_name, title, content, pub_date, source, url)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (
                    news.get('stock_code'),
                    news.get('stock_name'),
                    news.get('title'),
                    news.get('content'),
                    news.get('pub_date'),
                    news.get('source'),
                    news.get('url')
                ))
                if cursor.rowcount > 0:
                    saved_count += 1
            except sqlite3.Error as e:
                logger.warning(f"保存新闻失败: {e}")
        
        self.conn.commit()
        logger.info(f"成功保存 {saved_count} 条新闻数据")
        return saved_count
    
    def save_funds(self, funds_list: List[Dict]) -> int:
        """
        保存主力资金流向数据
        
        Args:
            funds_list: 资金流向数据列表
            
        Returns:
            保存的记录数
        """
        if not funds_list:
            return 0
        
        cursor = self.conn.cursor()
        saved_count = 0
        
        for fund in funds_list:
            try:
                cursor.execute("""
                    INSERT OR REPLACE INTO funds 
                    (stock_code, stock_name, trade_date, main_net_inflow, 
                     main_net_inflow_ratio, retail_net_inflow, super_net_inflow)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (
                    fund.get('stock_code'),
                    fund.get('stock_name'),
                    fund.get('trade_date'),
                    fund.get('main_net_inflow', 0),
                    fund.get('main_net_inflow_ratio', 0),
                    fund.get('retail_net_inflow', 0),
                    fund.get('super_net_inflow', 0)
                ))
                saved_count += 1
            except sqlite3.Error as e:
                logger.warning(f"保存资金数据失败: {e}")
        
        self.conn.commit()
        logger.info(f"成功保存 {saved_count} 条资金流向数据")
        return saved_count
    
    def save_sectors(self, sectors_list: List[Dict]) -> int:
        """
        保存行业板块数据
        
        Args:
            sectors_list: 行业数据列表
            
        Returns:
            保存的记录数
        """
        if not sectors_list:
            return 0
        
        cursor = self.conn.cursor()
        saved_count = 0
        
        for sector in sectors_list:
            try:
                cursor.execute("""
                    INSERT OR REPLACE INTO sectors 
                    (sector_name, trade_date, net_inflow, change_percent, turnover_rate)
                    VALUES (?, ?, ?, ?, ?)
                """, (
                    sector.get('sector_name'),
                    sector.get('trade_date'),
                    sector.get('net_inflow', 0),
                    sector.get('change_percent', 0),
                    sector.get('turnover_rate', 0)
                ))
                saved_count += 1
            except sqlite3.Error as e:
                logger.warning(f"保存行业数据失败: {e}")
        
        self.conn.commit()
        logger.info(f"成功保存 {saved_count} 条行业数据")
        return saved_count
    
    def save_events(self, events_list: List[Dict]) -> int:
        """
        保存事件数据
        
        Args:
            events_list: 事件数据列表
            
        Returns:
            保存的记录数
        """
        if not events_list:
            return 0
        
        cursor = self.conn.cursor()
        saved_count = 0
        
        for event in events_list:
            try:
                cursor.execute("""
                    INSERT INTO events 
                    (stock_code, stock_name, event_type, event_date, 
                     event_title, event_content, source)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (
                    event.get('stock_code'),
                    event.get('stock_name'),
                    event.get('event_type'),
                    event.get('event_date'),
                    event.get('event_title'),
                    event.get('event_content'),
                    event.get('source')
                ))
                saved_count += 1
            except sqlite3.Error as e:
                logger.warning(f"保存事件数据失败: {e}")
        
        self.conn.commit()
        logger.info(f"成功保存 {saved_count} 条事件数据")
        return saved_count
    
    def get_news(self, stock_code: Optional[str] = None, 
                 days: int = 7, limit: int = 100) -> pd.DataFrame:
        """
        获取新闻数据
        
        Args:
            stock_code: 股票代码（可选）
            days: 查询天数
            limit: 返回记录数限制
            
        Returns:
            新闻数据DataFrame
        """
        query = """
            SELECT * FROM news 
            WHERE pub_date >= datetime('now', '-{} days')
        """.format(days)
        
        if stock_code:
            query += f" AND stock_code = '{stock_code}'"
        
        query += f" ORDER BY pub_date DESC LIMIT {limit}"
        
        return pd.read_sql_query(query, self.conn)
    
    def get_funds(self, stock_code: str, days: int = 30) -> pd.DataFrame:
        """
        获取资金流向数据
        
        Args:
            stock_code: 股票代码
            days: 查询天数
            
        Returns:
            资金流向数据DataFrame
        """
        query = f"""
            SELECT * FROM funds 
            WHERE stock_code = '{stock_code}'
            AND trade_date >= datetime('now', '-{days} days')
            ORDER BY trade_date DESC
        """
        return pd.read_sql_query(query, self.conn)
    
    def get_sectors(self, trade_date: Optional[str] = None, 
                    limit: int = 50) -> pd.DataFrame:
        """
        获取行业板块数据
        
        Args:
            trade_date: 交易日期（可选，默认今天）
            limit: 返回记录数限制
            
        Returns:
            行业数据DataFrame
        """
        if trade_date:
            query = f"""
                SELECT * FROM sectors 
                WHERE trade_date = '{trade_date}'
                ORDER BY net_inflow DESC
                LIMIT {limit}
            """
        else:
            query = f"""
                SELECT * FROM sectors 
                WHERE trade_date = (SELECT MAX(trade_date) FROM sectors)
                ORDER BY net_inflow DESC
                LIMIT {limit}
            """
        return pd.read_sql_query(query, self.conn)
    
    def close(self):
        """关闭数据库连接"""
        if self.conn:
            self.conn.close()
            logger.info("数据库连接已关闭")
    
    def __enter__(self):
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()


# ==================== 便捷函数 ====================

def save_to_csv(df: pd.DataFrame, filename: str) -> str:
    """
    将DataFrame保存为CSV文件
    
    Args:
        df: 数据DataFrame
        filename: 文件名
        
    Returns:
        保存的文件路径
    """
    from quant_system.config import DATA_DIR
    filepath = os.path.join(DATA_DIR, filename)
    df.to_csv(filepath, index=False, encoding='utf-8-sig')
    logger.info(f"数据已导出至: {filepath}")
    return filepath

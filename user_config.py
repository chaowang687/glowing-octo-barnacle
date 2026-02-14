#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
用户配置管理模块
实现用户配置的持久化存储和加载
"""

import os
import json
from typing import Dict, Optional, Any


class UserConfig:
    """
    用户配置管理器
    """
    
    def __init__(self, config_file: str = './user_config.json'):
        """
        初始化用户配置管理器
        
        Args:
            config_file: 配置文件路径
        """
        self.config_file = config_file
        self.config = self._load_config()
    
    def _load_config(self) -> Dict[str, Any]:
        """
        加载配置文件
        
        Returns:
            配置字典
        """
        if os.path.exists(self.config_file):
            try:
                with open(self.config_file, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except Exception as e:
                from logger import exception
                exception(f"加载配置文件失败: {e}")
                return self._get_default_config()
        else:
            return self._get_default_config()
    
    def _get_default_config(self) -> Dict[str, Any]:
        """
        获取默认配置
        
        Returns:
            默认配置字典
        """
        return {
            'filter_conditions': {
                'min_change': 2,
                'min_volume': 0.5,
                'use_ma_filter': True,
                'use_macd_filter': False,
                'use_kdj_filter': False,
            },
            'display_options': {
                'show_all_stocks': False,
                'page_size': 20,
                'sort_by': '涨跌幅',
                'sort_ascending': False,
            },
            'analysis_settings': {
                'bi_threshold': 0.03,
                'use_macd': True,
                'show_indicators': True,
            },
            'recent_stocks': [],
            'theme': 'dark',
            'api_keys': {
                'deepseek': ''
            },
            'wechat_config': {
                'corpid': '',
                'corpsecret': '',
                'agentid': '',
                'user_id': ''
            },
        }
    
    def save(self):
        """
        保存配置到文件
        """
        try:
            # 创建目录（如果不存在）
            os.makedirs(os.path.dirname(self.config_file), exist_ok=True)
            
            with open(self.config_file, 'w', encoding='utf-8') as f:
                json.dump(self.config, f, indent=2, ensure_ascii=False)
        except Exception as e:
            from logger import exception
            exception(f"保存配置文件失败: {e}")
    
    def get(self, key: str, default: Any = None) -> Any:
        """
        获取配置值
        
        Args:
            key: 配置键，可以使用点号分隔获取嵌套值
            default: 默认值
        
        Returns:
            配置值
        """
        keys = key.split('.')
        value = self.config
        
        for k in keys:
            if isinstance(value, dict) and k in value:
                value = value[k]
            else:
                return default
        
        return value
    
    def set(self, key: str, value: Any):
        """
        设置配置值
        
        Args:
            key: 配置键，可以使用点号分隔设置嵌套值
            value: 配置值
        """
        keys = key.split('.')
        config = self.config
        
        # 遍历到倒数第二个键
        for k in keys[:-1]:
            if k not in config:
                config[k] = {}
            config = config[k]
        
        # 设置值
        config[keys[-1]] = value
        self.save()
    
    def get_filter_conditions(self) -> Dict[str, Any]:
        """
        获取筛选条件配置
        
        Returns:
            筛选条件配置字典
        """
        return self.get('filter_conditions', {})
    
    def set_filter_conditions(self, conditions: Dict[str, Any]):
        """
        设置筛选条件配置
        
        Args:
            conditions: 筛选条件配置字典
        """
        self.set('filter_conditions', conditions)
    
    def get_display_options(self) -> Dict[str, Any]:
        """
        获取显示选项配置
        
        Returns:
            显示选项配置字典
        """
        return self.get('display_options', {})
    
    def set_display_options(self, options: Dict[str, Any]):
        """
        设置显示选项配置
        
        Args:
            options: 显示选项配置字典
        """
        self.set('display_options', options)
    
    def get_analysis_settings(self) -> Dict[str, Any]:
        """
        获取分析设置配置
        
        Returns:
            分析设置配置字典
        """
        return self.get('analysis_settings', {})
    
    def set_analysis_settings(self, settings: Dict[str, Any]):
        """
        设置分析设置配置
        
        Args:
            settings: 分析设置配置字典
        """
        self.set('analysis_settings', settings)
    
    def add_recent_stock(self, stock_code: str, stock_name: str):
        """
        添加最近访问的股票
        
        Args:
            stock_code: 股票代码
            stock_name: 股票名称
        """
        recent_stocks = self.get('recent_stocks', [])
        
        # 移除已存在的相同股票
        recent_stocks = [(code, name) for code, name in recent_stocks if code != stock_code]
        
        # 添加到开头
        recent_stocks.insert(0, (stock_code, stock_name))
        
        # 限制数量
        recent_stocks = recent_stocks[:10]
        
        self.set('recent_stocks', recent_stocks)
    
    def get_recent_stocks(self) -> list:
        """
        获取最近访问的股票
        
        Returns:
            最近访问的股票列表
        """
        return self.get('recent_stocks', [])
    
    def get_deepseek_api_key(self) -> str:
        """
        获取DeepSeek API密钥
        
        Returns:
            DeepSeek API密钥
        """
        return self.get('api_keys.deepseek', '')
    
    def set_deepseek_api_key(self, api_key: str):
        """
        设置DeepSeek API密钥
        
        Args:
            api_key: DeepSeek API密钥
        """
        self.set('api_keys.deepseek', api_key)
    
    def get_wechat_config(self) -> Dict[str, str]:
        """
        获取企业微信配置
        
        Returns:
            企业微信配置字典
        """
        return self.get('wechat_config', {
            'corpid': '',
            'corpsecret': '',
            'agentid': '',
            'user_id': ''
        })
    
    def set_wechat_config(self, config: Dict[str, str]):
        """
        设置企业微信配置
        
        Args:
            config: 企业微信配置字典
        """
        self.set('wechat_config', config)


# 全局用户配置实例
user_config = UserConfig()


# 便捷函数
def get_user_config() -> UserConfig:
    """
    获取用户配置实例
    
    Returns:
        用户配置实例
    """
    return user_config


def load_user_config() -> Dict[str, Any]:
    """
    加载用户配置
    
    Returns:
        用户配置字典
    """
    return user_config.config


def save_user_config():
    """
    保存用户配置
    """
    user_config.save()

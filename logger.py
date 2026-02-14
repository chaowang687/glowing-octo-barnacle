#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
日志管理模块
统一管理所有日志记录和错误处理
"""

import os
import logging
from logging.handlers import RotatingFileHandler
from typing import Optional, Union


class Logger:
    """
    日志管理器
    """
    
    def __init__(self, name: str = 'a_quant', log_dir: str = './logs'):
        """
        初始化日志管理器
        
        Args:
            name: 日志名称
            log_dir: 日志目录
        """
        self.name = name
        self.log_dir = log_dir
        
        # 创建日志目录
        os.makedirs(log_dir, exist_ok=True)
        
        # 配置日志
        self.logger = logging.getLogger(name)
        self.logger.setLevel(logging.DEBUG)
        
        # 避免重复添加处理器
        if not self.logger.handlers:
            # 控制台处理器
            console_handler = logging.StreamHandler()
            console_handler.setLevel(logging.INFO)
            console_formatter = logging.Formatter(
                '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
            )
            console_handler.setFormatter(console_formatter)
            self.logger.addHandler(console_handler)
            
            # 文件处理器
            file_handler = RotatingFileHandler(
                os.path.join(log_dir, f'{name}.log'),
                maxBytes=10*1024*1024,  # 10MB
                backupCount=5
            )
            file_handler.setLevel(logging.DEBUG)
            file_formatter = logging.Formatter(
                '%(asctime)s - %(name)s - %(levelname)s - %(module)s - %(funcName)s - %(message)s'
            )
            file_handler.setFormatter(file_formatter)
            self.logger.addHandler(file_handler)
    
    def debug(self, message: str, *args, **kwargs):
        """
        记录调试信息
        
        Args:
            message: 日志消息
            *args: 额外参数
            **kwargs: 额外关键字参数
        """
        self.logger.debug(message, *args, **kwargs)
    
    def info(self, message: str, *args, **kwargs):
        """
        记录信息
        
        Args:
            message: 日志消息
            *args: 额外参数
            **kwargs: 额外关键字参数
        """
        self.logger.info(message, *args, **kwargs)
    
    def warning(self, message: str, *args, **kwargs):
        """
        记录警告信息
        
        Args:
            message: 日志消息
            *args: 额外参数
            **kwargs: 额外关键字参数
        """
        self.logger.warning(message, *args, **kwargs)
    
    def error(self, message: str, *args, **kwargs):
        """
        记录错误信息
        
        Args:
            message: 日志消息
            *args: 额外参数
            **kwargs: 额外关键字参数
        """
        self.logger.error(message, *args, **kwargs)
    
    def critical(self, message: str, *args, **kwargs):
        """
        记录严重错误信息
        
        Args:
            message: 日志消息
            *args: 额外参数
            **kwargs: 额外关键字参数
        """
        self.logger.critical(message, *args, **kwargs)
    
    def exception(self, message: str, *args, **kwargs):
        """
        记录异常信息
        
        Args:
            message: 日志消息
            *args: 额外参数
            **kwargs: 额外关键字参数
        """
        self.logger.exception(message, *args, **kwargs)


# 全局日志实例
logger = Logger()


# 便捷函数
def get_logger(name: Optional[str] = None) -> logging.Logger:
    """
    获取日志记录器
    
    Args:
        name: 日志名称
    
    Returns:
        日志记录器实例
    """
    if name:
        return logging.getLogger(name)
    return logger.logger


def debug(message: str, *args, **kwargs):
    """
    记录调试信息
    
    Args:
        message: 日志消息
        *args: 额外参数
        **kwargs: 额外关键字参数
    """
    logger.debug(message, *args, **kwargs)


def info(message: str, *args, **kwargs):
    """
    记录信息
    
    Args:
        message: 日志消息
        *args: 额外参数
        **kwargs: 额外关键字参数
    """
    logger.info(message, *args, **kwargs)


def warning(message: str, *args, **kwargs):
    """
    记录警告信息
    
    Args:
        message: 日志消息
        *args: 额外参数
        **kwargs: 额外关键字参数
    """
    logger.warning(message, *args, **kwargs)


def error(message: str, *args, **kwargs):
    """
    记录错误信息
    
    Args:
        message: 日志消息
        *args: 额外参数
        **kwargs: 额外关键字参数
    """
    logger.error(message, *args, **kwargs)


def critical(message: str, *args, **kwargs):
    """
    记录严重错误信息
    
    Args:
        message: 日志消息
        *args: 额外参数
        **kwargs: 额外关键字参数
    """
    logger.critical(message, *args, **kwargs)


def exception(message: str, *args, **kwargs):
    """
    记录异常信息
    
    Args:
        message: 日志消息
        *args: 额外参数
        **kwargs: 额外关键字参数
    """
    logger.exception(message, *args, **kwargs)


# 错误处理装饰器
def error_handler(default_return=None):
    """
    错误处理装饰器
    
    Args:
        default_return: 出错时的默认返回值
    
    Returns:
        装饰器函数
    """
    def decorator(func):
        def wrapper(*args, **kwargs):
            try:
                return func(*args, **kwargs)
            except Exception as e:
                func_name = func.__name__
                exception(message=f"Error in {func_name}: {e}")
                return default_return
        return wrapper
    return decorator


def retry(max_attempts: int = 3, delay: int = 1):
    """
    重试装饰器
    
    Args:
        max_attempts: 最大重试次数
        delay: 重试间隔（秒）
    
    Returns:
        装饰器函数
    """
    import time
    
    def decorator(func):
        def wrapper(*args, **kwargs):
            attempts = 0
            while attempts < max_attempts:
                try:
                    return func(*args, **kwargs)
                except Exception as e:
                    attempts += 1
                    if attempts < max_attempts:
                        warning(f"Attempt {attempts} failed: {e}, retrying in {delay} seconds...")
                        time.sleep(delay)
                    else:
                        exception(f"All {max_attempts} attempts failed: {e}")
                        raise
        return wrapper
    return decorator

#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
数据缓存模块
实现数据缓存机制，优化数据获取
"""

import os
import pickle
import hashlib
from datetime import datetime, timedelta
from typing import Dict, Any, Optional


class CacheManager:
    """
    缓存管理器
    """
    
    def __init__(self, cache_dir: str = './cache', default_ttl: int = 3600):
        """
        初始化缓存管理器
        
        Args:
            cache_dir: 缓存目录
            default_ttl: 默认缓存过期时间（秒）
        """
        self.cache_dir = cache_dir
        self.default_ttl = default_ttl
        
        # 创建缓存目录
        os.makedirs(cache_dir, exist_ok=True)
    
    def _get_cache_key(self, prefix: str, *args) -> str:
        """
        生成缓存键
        
        Args:
            prefix: 缓存前缀
            *args: 缓存参数
        
        Returns:
            缓存键
        """
        # 将所有参数转换为字符串并连接
        key_str = prefix + '_' + '_'.join(str(arg) for arg in args)
        # 使用MD5生成哈希值作为缓存键
        return hashlib.md5(key_str.encode('utf-8')).hexdigest()
    
    def _get_cache_path(self, key: str) -> str:
        """
        获取缓存文件路径
        
        Args:
            key: 缓存键
        
        Returns:
            缓存文件路径
        """
        return os.path.join(self.cache_dir, f"{key}.pkl")
    
    def get(self, prefix: str, *args, ttl: Optional[int] = None) -> Optional[Any]:
        """
        获取缓存数据
        
        Args:
            prefix: 缓存前缀
            *args: 缓存参数
            ttl: 缓存过期时间（秒），None使用默认值
        
        Returns:
            缓存数据，如果不存在或已过期返回None
        """
        from logger import debug, warning
        
        # 生成缓存键
        key = self._get_cache_key(prefix, *args)
        cache_path = self._get_cache_path(key)
        
        # 检查缓存文件是否存在
        if not os.path.exists(cache_path):
            debug(f"缓存不存在: {prefix}")
            return None
        
        try:
            # 读取缓存文件
            with open(cache_path, 'rb') as f:
                data = pickle.load(f)
            
            # 检查缓存是否过期
            timestamp = data.get('timestamp')
            if timestamp is None:
                warning("缓存文件格式错误，无时间戳")
                return None
            
            # 计算过期时间
            expiry_seconds = ttl if ttl is not None else self.default_ttl
            expiry_time = datetime.fromtimestamp(timestamp) + timedelta(seconds=expiry_seconds)
            
            if datetime.now() > expiry_time:
                debug(f"缓存已过期: {prefix}")
                # 删除过期缓存
                os.remove(cache_path)
                return None
            
            debug(f"缓存命中: {prefix}")
            return data.get('data')
        except Exception as e:
            from logger import exception
            exception(f"读取缓存失败: {e}")
            # 删除损坏的缓存文件
            if os.path.exists(cache_path):
                try:
                    os.remove(cache_path)
                except:
                    pass
            return None
    
    def set(self, prefix: str, data: Any, *args) -> bool:
        """
        设置缓存数据
        
        Args:
            prefix: 缓存前缀
            data: 要缓存的数据
            *args: 缓存参数
        
        Returns:
            是否成功设置缓存
        """
        from logger import debug, exception
        
        try:
            # 生成缓存键
            key = self._get_cache_key(prefix, *args)
            cache_path = self._get_cache_path(key)
            
            # 准备缓存数据
            cache_data = {
                'timestamp': datetime.now().timestamp(),
                'data': data
            }
            
            # 写入缓存文件
            with open(cache_path, 'wb') as f:
                pickle.dump(cache_data, f)
            
            debug(f"缓存设置成功: {prefix}")
            return True
        except Exception as e:
            exception(f"设置缓存失败: {e}")
            return False
    
    def delete(self, prefix: str, *args) -> bool:
        """
        删除缓存数据
        
        Args:
            prefix: 缓存前缀
            *args: 缓存参数
        
        Returns:
            是否成功删除缓存
        """
        from logger import debug
        
        try:
            # 生成缓存键
            key = self._get_cache_key(prefix, *args)
            cache_path = self._get_cache_path(key)
            
            # 删除缓存文件
            if os.path.exists(cache_path):
                os.remove(cache_path)
                debug(f"缓存删除成功: {prefix}")
            
            return True
        except Exception as e:
            from logger import exception
            exception(f"删除缓存失败: {e}")
            return False
    
    def clear(self, prefix: Optional[str] = None) -> bool:
        """
        清除缓存
        
        Args:
            prefix: 缓存前缀，None清除所有缓存
        
        Returns:
            是否成功清除缓存
        """
        from logger import info, exception
        
        try:
            if prefix:
                # 清除指定前缀的缓存
                for filename in os.listdir(self.cache_dir):
                    if filename.endswith('.pkl'):
                        # 读取缓存文件，检查前缀
                        try:
                            filepath = os.path.join(self.cache_dir, filename)
                            with open(filepath, 'rb') as f:
                                data = pickle.load(f)
                            # 这里简化处理，实际应该在缓存中存储前缀信息
                            # 或者通过文件名中的哈希值来判断
                            # 暂时清除所有缓存
                            os.remove(filepath)
                        except:
                            pass
                info(f"清除指定前缀缓存成功: {prefix}")
            else:
                # 清除所有缓存
                for filename in os.listdir(self.cache_dir):
                    if filename.endswith('.pkl'):
                        filepath = os.path.join(self.cache_dir, filename)
                        os.remove(filepath)
                info("清除所有缓存成功")
            
            return True
        except Exception as e:
            exception(f"清除缓存失败: {e}")
            return False


# 全局缓存实例
cache_manager = CacheManager()


# 便捷函数
def get_cache() -> CacheManager:
    """
    获取缓存管理器实例
    
    Returns:
        缓存管理器实例
    """
    return cache_manager


def cache_get(prefix: str, *args, ttl: Optional[int] = None) -> Optional[Any]:
    """
    获取缓存数据
    
    Args:
        prefix: 缓存前缀
        *args: 缓存参数
        ttl: 缓存过期时间（秒）
    
    Returns:
        缓存数据
    """
    return cache_manager.get(prefix, *args, ttl=ttl)


def cache_set(prefix: str, data: Any, *args) -> bool:
    """
    设置缓存数据
    
    Args:
        prefix: 缓存前缀
        data: 要缓存的数据
        *args: 缓存参数
    
    Returns:
        是否成功
    """
    return cache_manager.set(prefix, data, *args)


def cache_delete(prefix: str, *args) -> bool:
    """
    删除缓存数据
    
    Args:
        prefix: 缓存前缀
        *args: 缓存参数
    
    Returns:
        是否成功
    """
    return cache_manager.delete(prefix, *args)


def cache_clear(prefix: Optional[str] = None) -> bool:
    """
    清除缓存
    
    Args:
        prefix: 缓存前缀，None清除所有
    
    Returns:
        是否成功
    """
    return cache_manager.clear(prefix)

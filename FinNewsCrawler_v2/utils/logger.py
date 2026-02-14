# -*- coding: utf-8 -*-
"""
日志工具模块
使用loguru提供简洁的日志功能
"""

import sys
from loguru import logger
from config import LOG_CONFIG, BASE_DIR
import os

# 移除默认处理器
logger.remove()

# 添加控制台处理器
logger.add(
    sys.stdout,
    level=LOG_CONFIG["level"],
    format=LOG_CONFIG["format"],
    colorize=True,
)

# 添加文件处理器
log_dir = os.path.join(BASE_DIR, 'logs')
os.makedirs(log_dir, exist_ok=True)

logger.add(
    os.path.join(log_dir, 'crawler_{time:YYYY-MM-DD}.log'),
    level=LOG_CONFIG["level"],
    format=LOG_CONFIG["format"],
    rotation=LOG_CONFIG["rotation"],
    retention=LOG_CONFIG["retention"],
    encoding=LOG_CONFIG["encoding"],
    compression="zip",
)

# 设置全局日志级别
logger.level(name=LOG_CONFIG["level"])

__all__ = ["logger"]

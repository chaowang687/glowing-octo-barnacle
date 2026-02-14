#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
企业微信发送模块
用于通过企业微信发送股票分析报告
"""

import requests
import json
import os
from datetime import datetime

class WeChatSender:
    """
    企业微信发送器
    """
    
    def __init__(self, corpid=None, corpsecret=None, agentid=None):
        """
        初始化企业微信发送器
        
        Args:
            corpid: 企业ID
            corpsecret: 应用密钥
            agentid: 应用ID
        """
        self.corpid = corpid
        self.corpsecret = corpsecret
        self.agentid = agentid
        self.access_token = None
        self.token_expire_time = 0
    
    def get_access_token(self):
        """
        获取企业微信访问令牌
        
        Returns:
            str: 访问令牌
        """
        try:
            # 检查令牌是否有效
            current_time = datetime.now().timestamp()
            if self.access_token and current_time < self.token_expire_time:
                return self.access_token
            
            # 获取新令牌
            url = f"https://qyapi.weixin.qq.com/cgi-bin/gettoken"
            params = {
                "corpid": self.corpid,
                "corpsecret": self.corpsecret
            }
            
            response = requests.get(url, params=params, timeout=10)
            data = response.json()
            
            if data.get("errcode") == 0:
                self.access_token = data.get("access_token")
                self.token_expire_time = current_time + data.get("expires_in", 7200) - 600  # 提前10分钟刷新
                return self.access_token
            else:
                print(f"获取access_token失败: {data.get('errmsg')}")
                return None
                
        except Exception as e:
            print(f"获取access_token错误: {e}")
            return None
    
    def send_file_to_user(self, user_id, file_path, message="股票分析报告"):
        """
        发送文件给指定用户
        
        Args:
            user_id: 用户ID
            file_path: 文件路径
            message: 消息内容
        
        Returns:
            bool: 是否发送成功
        """
        try:
            # 获取访问令牌
            access_token = self.get_access_token()
            if not access_token:
                return False
            
            # 上传文件
            upload_url = f"https://qyapi.weixin.qq.com/cgi-bin/media/upload"
            params = {
                "access_token": access_token,
                "type": "file"
            }
            
            with open(file_path, "rb") as f:
                files = {
                    "media": (os.path.basename(file_path), f)
                }
                response = requests.post(upload_url, params=params, files=files, timeout=30)
            
            upload_data = response.json()
            if upload_data.get("errcode") != 0:
                print(f"上传文件失败: {upload_data.get('errmsg')}")
                return False
            
            media_id = upload_data.get("media_id")
            if not media_id:
                print("获取media_id失败")
                return False
            
            # 发送文件消息
            send_url = f"https://qyapi.weixin.qq.com/cgi-bin/message/send"
            params = {
                "access_token": access_token
            }
            
            payload = {
                "touser": user_id,
                "msgtype": "file",
                "agentid": self.agentid,
                "file": {
                    "media_id": media_id
                },
                "safe": 0,
                "enable_duplicate_check": 0
            }
            
            response = requests.post(send_url, params=params, json=payload, timeout=10)
            send_data = response.json()
            
            if send_data.get("errcode") == 0:
                return True
            else:
                print(f"发送消息失败: {send_data.get('errmsg')}")
                return False
                
        except Exception as e:
            print(f"发送文件错误: {e}")
            return False
    
    def send_file_to_department(self, department_id, file_path, message="股票分析报告"):
        """
        发送文件给指定部门
        
        Args:
            department_id: 部门ID
            file_path: 文件路径
            message: 消息内容
        
        Returns:
            bool: 是否发送成功
        """
        try:
            # 获取访问令牌
            access_token = self.get_access_token()
            if not access_token:
                return False
            
            # 上传文件
            upload_url = f"https://qyapi.weixin.qq.com/cgi-bin/media/upload"
            params = {
                "access_token": access_token,
                "type": "file"
            }
            
            with open(file_path, "rb") as f:
                files = {
                    "media": (os.path.basename(file_path), f)
                }
                response = requests.post(upload_url, params=params, files=files, timeout=30)
            
            upload_data = response.json()
            if upload_data.get("errcode") != 0:
                print(f"上传文件失败: {upload_data.get('errmsg')}")
                return False
            
            media_id = upload_data.get("media_id")
            if not media_id:
                print("获取media_id失败")
                return False
            
            # 发送文件消息
            send_url = f"https://qyapi.weixin.qq.com/cgi-bin/message/send"
            params = {
                "access_token": access_token
            }
            
            payload = {
                "toparty": department_id,
                "msgtype": "file",
                "agentid": self.agentid,
                "file": {
                    "media_id": media_id
                },
                "safe": 0,
                "enable_duplicate_check": 0
            }
            
            response = requests.post(send_url, params=params, json=payload, timeout=10)
            send_data = response.json()
            
            if send_data.get("errcode") == 0:
                return True
            else:
                print(f"发送消息失败: {send_data.get('errmsg')}")
                return False
                
        except Exception as e:
            print(f"发送文件错误: {e}")
            return False

def get_wechat_sender(corpid, corpsecret, agentid):
    """
    获取企业微信发送器实例
    
    Args:
        corpid: 企业ID
        corpsecret: 应用密钥
        agentid: 应用ID
    
    Returns:
        WeChatSender: 发送器实例
    """
    return WeChatSender(corpid, corpsecret, agentid)

#!/usr/bin/env python3
"""
启动Streamlit服务器，绕过邮箱输入提示
"""

import os
import subprocess
import time

def start_streamlit():
    """启动Streamlit服务器"""
    # 设置环境变量
    env = os.environ.copy()
    env['STREAMLIT_BROWSER_GATHER_USAGE_STATS'] = 'false'
    env['STREAMLIT_EMAIL'] = ''
    
    # 启动命令
    cmd = [
        'python3', '-m', 'streamlit', 'run', 'app_v2.py',
        '--server.port', '8501',
        '--server.headless', 'false'
    ]
    
    print("🚀 正在启动Streamlit服务器...")
    print(f"执行命令: {' '.join(cmd)}")
    
    # 启动进程
    process = subprocess.Popen(
        cmd,
        env=env,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True
    )
    
    # 读取输出
    try:
        while True:
            line = process.stdout.readline()
            if not line:
                break
            
            # 打印输出
            print(line.strip())
            
            # 检查是否启动成功
            if "Local URL:" in line:
                print("\n🎉 Streamlit服务器启动成功！")
                print("\n🌐 访问地址:")
                print(f"   {line.strip()}")
            elif "Network URL:" in line:
                print(f"   {line.strip()}")
                print("\n💡 提示: 请在浏览器中打开Local URL地址")
                break
            
            # 检查错误
            elif "error" in line.lower() or "exception" in line.lower():
                print(f"\n❌ 启动出错: {line.strip()}")
                break
                
    except KeyboardInterrupt:
        print("\n⏹️  正在停止服务器...")
        process.terminate()
    
    return process

if __name__ == "__main__":
    process = start_streamlit()
    # 保持脚本运行
    try:
        while process.poll() is None:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\n⏹️  服务器已停止")

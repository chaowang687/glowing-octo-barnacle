import streamlit as st
import json
from deepseek_analyzer import DeepSeekAnalyzer
from agent_tools import TOOLS_DESC, dispatch_tool
from user_config import get_user_config

# 页面配置
st.set_page_config(
    page_title="AI 助手",
    page_icon="🤖",
    layout="wide"
)

st.title("🤖 AI 量化助手")
st.caption("基于 DeepSeek V3/R1 · 支持查价/回测/市场分析")

# 初始化 Session State
if "messages" not in st.session_state:
    st.session_state.messages = []
    # 添加系统提示词（隐藏）
    st.session_state.messages.append({
        "role": "system", 
        "content": f"你是一个专业的量化交易助手。你可以回答用户关于股票、市场的问题。你有以下工具可供使用：\n{TOOLS_DESC}\n请严格遵守工具调用的JSON格式。"
    })

# 获取 API Key
user_config = get_user_config()
api_key = user_config.get_deepseek_api_key()

if not api_key:
    st.warning("⚠️ 请先在【设置】页面配置 DeepSeek API Key")
    st.stop()

analyzer = DeepSeekAnalyzer(api_key)

# 侧边栏：清空历史
with st.sidebar:
    if st.button("🗑️ 清空对话历史"):
        st.session_state.messages = [st.session_state.messages[0]] # 保留 System Prompt
        st.rerun()
    
    st.markdown("### 🛠️ 可用能力")
    st.markdown("- **查行情**: '看看现在的市场情况'")
    st.markdown("- **查个股**: '查询平安银行的价格'")
    st.markdown("- **做回测**: '帮我回测茅台，阈值60'")
    st.markdown("- **聊策略**: '介绍一下均值回归策略'")

# 显示历史消息 (跳过 System Prompt)
for msg in st.session_state.messages:
    if msg["role"] == "system":
        continue
    with st.chat_message(msg["role"]):
        st.markdown(msg["content"])

# 处理用户输入
if prompt := st.chat_input("输入你的问题..."):
    # 1. 显示用户消息
    st.session_state.messages.append({"role": "user", "content": prompt})
    with st.chat_message("user"):
        st.markdown(prompt)

    # 2. 调用 AI
    with st.chat_message("assistant"):
        message_placeholder = st.empty()
        full_response = ""
        
        with st.spinner("AI 正在思考..."):
            # 调用 Chat 接口
            # 注意：DeepSeek V3 不支持 streaming (requests 库实现也没做 stream)，所以是阻塞的
            response_content = analyzer.chat(st.session_state.messages)
            
            if not response_content:
                full_response = "❌ 调用 API 失败，请检查网络或 Key。"
                message_placeholder.markdown(full_response)
            else:
                # 检查是否包含 Tool Call
                # 简单的解析逻辑：检查是否包含 ```json ... ``` 且里面有 "tool" 字段
                tool_call_found = False
                try:
                    # 尝试寻找 JSON 代码块
                    import re
                    json_match = re.search(r'```json\s*(\{.*?\})\s*```', response_content, re.DOTALL)
                    if json_match:
                        json_str = json_match.group(1)
                        tool_data = json.loads(json_str)
                        
                        if "tool" in tool_data:
                            tool_call_found = True
                            tool_name = tool_data["tool"]
                            tool_params = tool_data.get("params", {})
                            
                            message_placeholder.markdown(f"🛠️ 正在调用工具: `{tool_name}` ...")
                            
                            # 执行工具
                            tool_result = dispatch_tool(tool_name, tool_params)
                            
                            # 将工具结果反馈给 AI
                            # 添加 AI 的 Tool Call 意图到历史
                            st.session_state.messages.append({"role": "assistant", "content": response_content})
                            # 添加工具结果到历史
                            tool_msg = {
                                "role": "user", 
                                "content": f"工具调用结果:\n{tool_result}\n请根据结果回答我的问题。"
                            }
                            st.session_state.messages.append(tool_msg)
                            
                            # 再次调用 AI 获取最终回答
                            final_response = analyzer.chat(st.session_state.messages)
                            full_response = final_response
                            
                    else:
                        full_response = response_content

                except Exception as e:
                    # 解析失败，直接显示原文
                    full_response = response_content
                
                if not tool_call_found:
                    full_response = response_content

                message_placeholder.markdown(full_response)
                
                # 如果没有发生工具调用循环（即 tool_call_found 为 False），则记录单次回复
                # 如果发生了（tool_call_found 为 True），上面的逻辑已经append了中间过程，现在append最终结果
                st.session_state.messages.append({"role": "assistant", "content": full_response})

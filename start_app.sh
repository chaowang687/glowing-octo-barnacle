#!/bin/bash
export STREAMLIT_BROWSER_GATHER_USAGE_STATS=false
export STREAMLIT_EMAIL=""
python3 -m streamlit run app_v2.py --server.headless false --server.port 8501
import requests
import json

base_url = "http://localhost:5283"
creds = {"username": "admin", "password": "Admin@2026"}

# 1. Login
print("Logging in...")
resp = requests.post(f"{base_url}/api/auth/admin-login", json=creds)
if resp.status_code != 200:
    print(f"Login failed: {resp.status_code} {resp.text}")
    exit(1)

token = resp.json()["token"]
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}

# 2. Get POI 2
print("Getting POI 2...")
resp = requests.get(f"{base_url}/api/poi/2", headers=headers)
poi = resp.json()

# 3. Modify (simulating the frontend update with translations)
poi["ownerInfo"] = "gs25 cao thắng"
# Ensure translations have all required fields (the new ones I added)
if "translations" in poi and poi["translations"]:
    for t in poi["translations"]:
        t["originalDescription"] = t.get("originalDescription") or poi["description"]

# 4. PUT Update
print("Updating POI 2...")
resp = requests.put(f"{base_url}/api/poi/2", headers=headers, json=poi)

print(f"Update Result Status: {resp.status_code}")
print("Update Result Body:")
try:
    print(json.dumps(resp.json(), indent=2))
except:
    print(resp.text)

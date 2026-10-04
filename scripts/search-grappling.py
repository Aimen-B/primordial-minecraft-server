import urllib.request
import urllib.parse
import json

base_url = "https://api.modrinth.com/v2/search"
params = {
    "query": "grappling hook",
    "facets": json.dumps([["categories:neoforge"], ["versions:1.21.1"]])
}
url = base_url + "?" + urllib.parse.urlencode(params)
req = urllib.request.Request(url, headers={"User-Agent": "Primordial/1.0"})
try:
    with urllib.request.urlopen(req) as resp:
        data = json.loads(resp.read().decode())
        print(f"Total hits: {data['total_hits']}")
        for hit in data['hits'][:8]:
            print(f"- Title: {hit['title']}, Slug: {hit['slug']}, Project ID: {hit['project_id']}, Downloads: {hit['downloads']}")
except Exception as e:
    print("Search error:", e)

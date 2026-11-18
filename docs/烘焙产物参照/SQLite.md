# SQLite 产物

文件：`dialogue.db`，**一个库装所有章**。

## 命名

| 表 | 名字 |
|---|---|
| 全局表 | `chapters`、`characters` |
| 章节表 | `{chapter_id}_nodes`、`{chapter_id}_edges`、`{chapter_id}_scenes` |

```sql
-- 连接级设置，建表和导入前都要开；详见「打开数据库」
PRAGMA foreign_keys = ON;

-- 全局
CREATE TABLE chapters (
  chapter_id      TEXT NOT NULL PRIMARY KEY,   -- SQLite 的文本主键不隐含 NOT NULL
  chapter_title   TEXT NOT NULL,
  entry_node      TEXT NOT NULL,
  next_chapter_id TEXT REFERENCES chapters(chapter_id) ON DELETE RESTRICT,  -- NULL 表示最后一章，外键不检查 NULL
  unlock_if       TEXT                         -- 批次 ②，暂空
);

CREATE TABLE characters (
  speaker_id       TEXT NOT NULL PRIMARY KEY,
  speaker_name     TEXT NOT NULL,
  portrait_default TEXT
);

-- 第 1 章
CREATE TABLE ch01_nodes (
  idx        INTEGER PRIMARY KEY,
  node_id    TEXT NOT NULL UNIQUE,
  type       TEXT NOT NULL,
  speaker_id TEXT REFERENCES characters(speaker_id) ON DELETE RESTRICT,
  zh_cn      TEXT,
  en_us      TEXT,
  note       TEXT,
  pos_x      INTEGER DEFAULT 0,
  pos_y      INTEGER DEFAULT 0
);
CREATE INDEX ix_ch01_nodes_speaker ON ch01_nodes(speaker_id);

CREATE TABLE ch01_edges (
  idx        INTEGER PRIMARY KEY,
  from_node  TEXT NOT NULL REFERENCES ch01_nodes(node_id) ON DELETE CASCADE,
  to_node    TEXT NOT NULL REFERENCES ch01_nodes(node_id) ON DELETE CASCADE,
  seq        INTEGER NOT NULL,
  once       INTEGER NOT NULL DEFAULT 0,
  visible_if TEXT,                     -- 批次 ②
  enabled_if TEXT,                     -- 批次 ②
  UNIQUE (from_node, seq)              -- 已自动生成 (from_node, seq) 索引，无需再建
);
-- 外键子表的列必须建索引：否则删/改父行时要做全表扫
CREATE INDEX ix_ch01_edges_to ON ch01_edges(to_node);

CREATE TABLE ch01_scenes (
  scene_id    TEXT NOT NULL PRIMARY KEY,
  entry_node  TEXT NOT NULL REFERENCES ch01_nodes(node_id) ON DELETE CASCADE,
  bgm_default TEXT
);
CREATE INDEX ix_ch01_scenes_entry ON ch01_scenes(entry_node);
```

第 2 章起照着复制一份，把 `ch01_` 换成 `ch02_`。

## 打开数据库

外键默认是**关**的，而且这类设置是**连接级**的，每个新连接都要执行。一套常用配置：

```csharp
var conn = new SqliteConnection("dialogue.db");
conn.ExecuteNonQuery("""
PRAGMA journal_mode = WAL;        -- 建库时设一次，写进库文件后永久有效
PRAGMA synchronous  = NORMAL;     -- WAL 下足够安全，比默认 FULL 快一个量级
PRAGMA busy_timeout = 5000;       -- 锁冲突等 5 秒再报错；默认 0 是立刻失败
PRAGMA foreign_keys = ON;         -- 外键，默认关
PRAGMA cache_size   = -32000;     -- 约 32MB 页缓存，负数是 KiB
PRAGMA mmap_size    = 268435456;  -- 256MB 内存映射，读多时更快
"""u8);
```

收口成一个入口，避免某处漏掉：

```csharp
public static class Db
{
    public static SqliteConnection Open(string path, bool readOnly = false)
    {
        var conn = new SqliteConnection(path);
        conn.ExecuteNonQuery("""
        PRAGMA journal_mode = WAL;
        PRAGMA synchronous  = NORMAL;
        PRAGMA busy_timeout = 5000;
        PRAGMA foreign_keys = ON;
        PRAGMA cache_size   = -32000;
        PRAGMA mmap_size    = 268435456;
        """u8);
        if (readOnly)
            conn.ExecuteNonQuery("PRAGMA query_only = ON;"u8);
        return conn;
    }
}
```

| PRAGMA | 生效范围 | 说明 |
|---|---|---|
| `journal_mode = WAL` | **写进库文件，设一次永久有效** | 读写互不阻塞。副作用是目录多出 `-wal` / `-shm`，备份打包要一起带走；不能放网络盘；只读打开时改不了 |
| `synchronous` | 连接级 | `NORMAL` 在 WAL 下足以防崩溃损坏，只是断电可能丢最后一个事务 |
| `busy_timeout` | 连接级 | 拿不到锁时等待的毫秒数，纯只读不需要 |
| `foreign_keys` | 连接级 | 外键开关 |
| `query_only` | 连接级 | 让连接变成只读，误写直接报错 |
| `cache_size` | 连接级 | 负值表示 KiB |
| `mmap_size` | 连接级 | 内存映射大小，小库可以不设 |

### 元数据

| PRAGMA | 用途 |
|---|---|
| `application_id` | 4 字节标记"这是对话系统的库"，打开陌生 db 时能识别 |
| `user_version` | 4 字节整数，存 schema 版本；bake 写入，运行时校验 |

`user_version` 相当于把版本号写进库文件本身，不需要额外的 sidecar 文件。

### 发版前

```sql
PRAGMA wal_checkpoint(TRUNCATE);   -- 把 -wal 里的数据合并回主文件
PRAGMA integrity_check;            -- 页面级自检
PRAGMA foreign_key_check;          -- 外键自检，返回空表示没有悬空引用
ANALYZE;                           -- 导入大量数据后更新统计信息
```

第一条最关键：WAL 模式下最近的写入可能还在 `-wal` 里，只拷 `dialogue.db` 会静默丢失数据。

长期运行的库可以用 `PRAGMA optimize;` 替代 `ANALYZE`——它只在统计信息过期时才真正干活。

### 不建议碰

| PRAGMA | 原因 |
|---|---|
| `locking_mode = EXCLUSIVE` | 省一点锁开销，但多进程访问会卡死 |
| `temp_store = MEMORY` | 查询不建临时表，无收益 |
| `secure_delete` | 给敏感数据用的，拖慢写入 |
| `journal_mode = MEMORY` / `OFF` | 崩溃会损坏库 |

## 外键约束

| 约束 | 能不能建成外键 |
|---|---|
| `ch01_nodes.speaker_id` → `characters.speaker_id` | ✅ 目标表固定 |
| `ch01_edges.from_node` / `to_node` → `ch01_nodes.node_id` | ✅ 同章内 |
| `ch01_scenes.entry_node` → `ch01_nodes.node_id` | ✅ 同章内 |
| `chapters.next_chapter_id` → `chapters.chapter_id` | ✅ 自引用；NULL 时外键不检查 |
| `chapters.entry_node` → 某章的 `nodes` | ❌ 目标表名随章变化 |

最后一条只能由 bake 校验，数据库层面表达不了。

另外记住一条 SQLite 的坑：**文本主键不隐含 `NOT NULL`**。`chapter_id TEXT PRIMARY KEY` 仍然可以插入 `NULL`，而且能插多条。所有 `TEXT` 主键都要显式写 `NOT NULL`——`INTEGER PRIMARY KEY` 不受影响，它指向 rowid。

`chapters.unlock_if`、`edges.visible_if` / `enabled_if` 指向 `conditions`，而 `conditions` 表属于批次 ②，现在还没有。在它建出来之前，这三列只是普通字符串，没有外键保护。

## 查询

**某个 choice 节点的选项，按显示顺序：**

```sql
SELECT e.seq, e.to_node, n.zh_cn AS label, e.once
FROM ch01_edges e
JOIN ch01_nodes n ON n.node_id = e.to_node
WHERE e.from_node = 'ch01_0005'
ORDER BY e.seq;
```

```
0|ch01_0006|（敲门）|0
1|ch01_0007|（低声）是我。|1
```

**没写文案的节点：**

```sql
SELECT node_id FROM ch01_nodes
WHERE type IN ('line', 'narration', 'choice')
  AND (zh_cn IS NULL OR zh_cn = '');
```

**分支最密的节点：**

```sql
SELECT from_node, COUNT(*) AS n
FROM ch01_edges GROUP BY from_node ORDER BY n DESC LIMIT 5;
```

**某一章的不可达节点：**

```sql
WITH RECURSIVE reach(id) AS (
  SELECT entry_node FROM chapters WHERE chapter_id = 'ch01'
  UNION
  SELECT e.to_node FROM ch01_edges e JOIN reach r ON e.from_node = r.id
)
SELECT node_id FROM ch01_nodes WHERE node_id NOT IN (SELECT id FROM reach);
```

**列出所有章的表清单：**

```sql
SELECT name FROM sqlite_master
WHERE type = 'table' AND name LIKE '%\_nodes' ESCAPE '\'
ORDER BY name;
```

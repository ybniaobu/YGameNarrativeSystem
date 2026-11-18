# Binary 产物格式

> 状态：设计草案 v0.1（2026-11-18）
> 所属：[对话系统总体设计.md](../对话系统总体设计.md) 第 11 节
> 目标：运行时直接加载的二进制格式，内部使用 CSR；不替代 JSON/SQLite 的工具用途。

## 1. 文件

一个 `story.bin` 装所有章：

```
story.bin
├── Header
├── String Table
├── Global Section
├── Chapter Directory
└── Chapter Blobs（按章连续存放）
```

- 固定小端序（little-endian）。
- 所有段 4 字节对齐。
- 所有字符串进入统一字符串表，段内只存 `string_ref`（u32，字符串表内偏移）。
- 所有索引都是 0 基数组下标。

## 2. Header

| 偏移 | 类型 | 字段 | 说明 |
|---:|---|---|---|
| 0 | `u8[4]` | `magic` | `YNRB` |
| 4 | `u16` | `format_version` | 当前 1 |
| 6 | `u16` | `schema_version` | 与 JSON/SQLite 一致，当前 1 |
| 8 | `u32` | `flags` | bit0：压缩；其余保留 |
| 12 | `u32` | `crc32` | 从 Header 之后到文件末尾的 CRC32 |
| 16 | `u32` | `string_table_offset` | 字符串表起始 |
| 20 | `u32` | `string_table_size` | 字符串表字节数 |
| 24 | `u32` | `global_offset` | 全局段起始 |
| 28 | `u32` | `global_size` | 全局段字节数 |
| 32 | `u32` | `chapter_directory_offset` | 章节目录起始 |
| 36 | `u32` | `chapter_count` | 章节数 |
| 40 | `u32` | `reserved` | 0 |

Header 固定 44 字节。

## 3. String Table

```
u32 string_count
entry[string_count]:
    u32 byte_length
    u8[byte_length] utf8
```

- 字符串不要求 NUL 结尾。
- 允许重复；烘焙时可选去重，去重顺序必须稳定（按首次使用顺序）。
- `string_ref` 指向 entry 的起始偏移（即 `byte_length` 字段的偏移）。
- 读取时校验 `string_ref + 4 + byte_length <= string_table_end`。

## 4. Global Section

```
u32 project_count
entry[project_count]:
    string_ref key
    string_ref value

u32 chapter_count
entry[chapter_count]:
    string_ref chapter_id
    string_ref chapter_title
    u32 sort_order
    string_ref entry_scene
    string_ref entry_node
    string_ref next_chapter_id
    string_ref unlock_if

u32 character_count
entry[character_count]:
    string_ref speaker_id
    string_ref speaker_name
    string_ref portrait_default
    string_ref color
    string_ref voice_prefix

u32 locale_count
entry[locale_count]:
    string_ref locale_id
    string_ref display_name
    u8 is_default
    u8[3] padding
    u32 sort_order

u32 variable_count
entry[variable_count]:
    string_ref var_name
    u8 type            // 0 int, 1 bool, 2 float, 3 string
    u8[3] padding
    string_ref default_value
    string_ref desc

u32 condition_count
entry[condition_count]:
    string_ref condition_id
    string_ref expr
    string_ref desc

u32 effect_count
entry[effect_count]:
    string_ref effect_id
    u32 seq
    u8 op              // 0 var_add, 1 var_set, 2 flag_set, 3 item_add, 4 unlock, 5 audio, 6 custom
    u8[3] padding
    string_ref target
    string_ref value
```

- `chapter_count` 在 Header 和 Global 段各出现一次，必须一致。
- `locale` 的 `is_default` 全表只能有一个 1。
- 字符串为空的字段写 `string_ref = 0xFFFFFFFF`（无效引用），读取时映射为 `null`。

## 5. Chapter Directory

```
u32 count
entry[count]:
    string_ref chapter_id
    u32 blob_offset
    u32 blob_size
```

- 顺序与 `chapters.sort_order` 一致。
- 运行时可以按 `chapter_id` 查目录，只加载需要的章。

## 6. Chapter Blob

```
string_ref chapter_id
u32 scene_count
u32 node_count
u32 edge_count
u32 text_count
u32 node_effect_count
```

### 6.1 Scenes

```
entry[scene_count]:
    string_ref scene_id
    string_ref scene_title
    u32 entry_node_index       // nodes 数组下标
    string_ref bgm_default
    string_ref background_default
    i32 next_scene_index        // -1 表示没有
    string_ref unlock_if
    string_ref note
```

### 6.2 Nodes

```
entry[node_count]:
    string_ref node_id
    i32 scene_index             // -1 表示未归属
    u8 type                     // 0 line, 1 narration, 2 choice, 3 random,
                                // 4 jump, 5 event, 6 condition, 7 end
    u8[3] padding
    string_ref speaker_id
    string_ref zh_cn
    string_ref note
    i32 pos_x
    i32 pos_y
    string_ref condition_id
    u32 text_start              // node_texts 起始下标
    u32 text_count
    u32 effect_start            // node_effects 起始下标
    u32 effect_count
```

- `text_start/text_count` 指向 6.4 的 `node_texts` 数组；没有翻译时 `count = 0`。
- `effect_start/effect_count` 指向 6.5 的 `node_effects` 数组。
- 默认语言文本始终在 `zh_cn`；`node_texts` 只放非默认语言。

### 6.3 Edges（CSR）

```
u32 row_ptr[node_count + 1]
u32 col_idx[edge_count]
EdgeMeta edge_meta[edge_count]:
    u32 seq
    string_ref label
    u32 weight
    u8 flags                    // bit0 once
    u8[3] padding
    string_ref visible_if
    string_ref enabled_if
```

- `row_ptr[i] .. row_ptr[i+1]` 是节点 `i` 的后继在 `col_idx` / `edge_meta` 中的范围。
- `col_idx` 里存目标节点的下标。
- 边按 `from_node_index` 升序、再按 `seq` 升序排列。
- 这样 `row_ptr` 天然满足 CSR，运行时零分配取后继。

### 6.4 Node Texts

```
entry[text_count]:
    u32 node_index
    u16 locale_index
    u16 reserved
    string_ref text
```

- 按 `node_index` 升序排列；同一节点内按 `locale_index` 升序。
- `locale_index` 指向 Global 段的 `locales` 数组。
- 默认语言的文本不放进这里。

### 6.5 Node Effects

```
entry[node_effect_count]:
    u32 node_index
    u32 effect_index
    u16 seq
    u16 reserved
```

- 按 `node_index` 升序；同一节点内按 `seq` 升序。
- `effect_index` 指向 Global 段的 `effects` 数组（注意 `effects` 是组，按 `(effect_id, seq)` 多行；这里存的是组的起始行或组索引，烘焙时确定）。
- 建议烘焙时把 `effects` 按组整理成 `effect_group` 数组，`effect_index` 指向组；组的 `seq` 行连续存放。

## 7. 对齐与填充

- 每个段起始 4 字节对齐。
- 每个 entry 的大小按字段自然对齐；`u8` 后补 3 字节，保证 `u32/string_ref` 对齐。
- 数组之间补 0 到 4 字节边界。
- 读取端不要依赖 C# 的 `StructLayout` 默认值；用显式偏移读取。

## 8. CRC 与校验

- `crc32` 覆盖 Header 之后到文件末尾的全部字节；Header 本身不参与。
- 加载时依次校验：
  1. `magic == "YNRB"`
  2. `format_version == 1`
  3. `schema_version` 与运行时期望一致
  4. 所有 offset/size 在文件范围内
  5. `string_ref` 在字符串表范围内
  6. `row_ptr` 单调不减，末项等于 `edge_count`
  7. 数组下标在范围内
- 任一失败直接抛错，不尝试修复。

## 9. 压缩

`flags` bit0：

- 0：不压缩。
- 1：除 Header 外的内容用 LZ4 block 压缩；Header 里另存解压后大小。

第一版可以不实现压缩，只保留 flag。

## 10. 确定性

同一份构建结果重复烘焙，`story.bin` 必须字节一致：

- 字符串去重按首次使用顺序。
- `chapters` 按 `sort_order`，再按 `chapter_id`。
- `scenes` 按 `scene_id`。
- `nodes` 按 `idx`。
- `edges` 按 `from_node_index`、`seq`。
- `node_texts` 按 `node_index`、`locale_index`。
- `node_effects` 按 `node_index`、`seq`。
- `effects` 按 `effect_id`、`seq`。
- 所有 padding 写 0。

## 11. 运行时加载

```csharp
public sealed class DialogueBinary
{
    public DialogueDatabase Database { get; }
    public DialogueGraph LoadChapter(string chapterId);
}
```

- 启动时 mmap/读入整个 `story.bin`，做一次校验。
- `DialogueGraph` 直接引用 `ReadOnlySpan<byte>` 上的数组视图，或拷成 `int[]`/`uint[]`。
- 章节按需加载：用目录 offset/size 定位 blob，不解析其他章。
- JSON/SQLite loader 也返回同一个 `DialogueGraph`，Runner 不感知格式。

## 12. 与 JSON/SQLite 的对应

| Binary | JSON | SQLite |
|---|---|---|
| Global 段 | `dialogue_global.json` | 全局表 |
| Chapter blob | `{chapter_id}.json` | `{chapter_id}_*` 表 |
| `row_ptr` / `col_idx` | `edges` 分组 | `{chapter_id}_edges` |
| `node_texts` | `nodes.*.texts` | `{chapter_id}_node_texts` |
| `node_effects` | `nodes.*.effects` | `{chapter_id}_node_effects` |

字段语义以 [对话系统总体设计.md](../对话系统总体设计.md) 为准；Binary 只是布局不同。

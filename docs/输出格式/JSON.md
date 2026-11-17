# JSON 产物

| 文件 | 表 |
|---|---|
| `dialogue_global.json` | `chapters` `characters` `variables` `conditions` `effects` |
| `{chapter_id}.json` | `nodes` `edges` `scenes` |

## 结构

每张表一个对象，**键 = 主键**，值 = 该行去掉主键列后的字段。

| 表 | 键 |
|---|---|
| `chapters` | `chapter_id` |
| `characters` | `speaker_id` |
| `variables` | `var_name` |
| `conditions` | `condition_id` |
| `effects` | `effect_id`（值是指令数组，一个 ID 多行） |
| `nodes` | `node_id` |
| `edges` | `from_node`（值是该节点的出边数组） |
| `scenes` | `scene_id` |

## 字段

| 表 | 除键外的字段 |
|---|---|
| `chapters` | `chapter_title` `entry_node` `next_chapter_id` `unlock_if` |
| `characters` | `speaker_name` `portrait_default` |
| `variables` | `type` `desc` |
| `conditions` | `expr` `desc` |
| `effects` | `seq` `op` `target` `value` |
| `nodes` | `type` `speaker_id` `zh_cn` `en_us` `note` `pos_x` `pos_y` |
| `edges` | `to_node` `seq` `once` `visible_if` `enabled_if` |
| `scenes` | `entry_node` `bgm_default` |

## 转换

| CSV | JSON |
|---|---|
| 空单元格 / `NULL` | `null` |
| `TRUE` / `FALSE` | `true` / `false` |
| 整数列（`seq`、`pos_x`、`pos_y`） | number |
| 空表 | `{}` |

## dialogue_global.json

```json
{
  "chapters": {
    "ch01": {"chapter_title": "第一章 雨夜", "entry_node": "ch01_0001"}
  },
  "characters": {
    "spk_001_lin": {"speaker_name": "琳", "portrait_default": "lin_default"},
    "spk_002_kai": {"speaker_name": "凯", "portrait_default": "kai_default"}
  },
  "variables": {},
  "conditions": {},
  "effects": {}
}
```

## {chapter_id}.json

`Sample/ch01`：32 节点 / 35 边 / 31 文案 / 2 片段。

```json
{
  "nodes": {
    "ch01_0001": {"type": "narration", "speaker_id": null, "zh_cn": "雨下了一整夜。", "en_us": "It rained all night.", "note": "开场", "pos_x": 0, "pos_y": 0},
    "ch01_0002": {"type": "narration", "speaker_id": null, "zh_cn": "你站在旧公寓门口，门牌上的漆已经剥落。", "en_us": "You stand at the door of the old apartment, the paint on the number plate long peeled away.", "note": null, "pos_x": 0, "pos_y": 100},
    "ch01_0003": {"type": "line", "speaker_id": "spk_002_kai", "zh_cn": "你确定是这里？", "en_us": "You sure this is the place?", "note": null, "pos_x": 0, "pos_y": 200},
    "ch01_0004": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "（门内）谁？", "en_us": "(From inside) Who is it?", "note": null, "pos_x": 0, "pos_y": 300},
    "ch01_0005": {"type": "choice", "speaker_id": null, "zh_cn": "你要怎么回答？", "en_us": "How do you answer?", "note": "回答方式", "pos_x": 0, "pos_y": 400},
    "ch01_0006": {"type": "narration", "speaker_id": null, "zh_cn": "（敲门）", "en_us": "(Knock)", "note": "选项 A", "pos_x": 200, "pos_y": 500},
    "ch01_0007": {"type": "narration", "speaker_id": null, "zh_cn": "（低声）是我。", "en_us": "(Quietly) It's me.", "note": "选项 B（只出现一次）", "pos_x": 400, "pos_y": 500},
    "ch01_0008": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "（门开了一条缝）……你终于来了。", "en_us": "(The door opens a crack) ...You finally came.", "note": null, "pos_x": 0, "pos_y": 600},
    "ch01_0009": {"type": "line", "speaker_id": "spk_002_kai", "zh_cn": "我在楼下等你。", "en_us": "I'll wait downstairs.", "note": null, "pos_x": 0, "pos_y": 700},
    "ch01_0010": {"type": "choice", "speaker_id": null, "zh_cn": "要不要让他一起进来？", "en_us": "Should we let him come in too?", "note": "要不要带凯进屋", "pos_x": 0, "pos_y": 800},
    "ch01_0011": {"type": "narration", "speaker_id": null, "zh_cn": "（拉住他的袖子）", "en_us": "(Grab his sleeve)", "note": "选项 A", "pos_x": 200, "pos_y": 900},
    "ch01_0012": {"type": "narration", "speaker_id": null, "zh_cn": "（让他留在楼下）", "en_us": "(Leave him downstairs)", "note": "选项 B", "pos_x": 400, "pos_y": 900},
    "ch01_0013": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "进来吧，两个都进来。", "en_us": "Come in. Both of you.", "note": null, "pos_x": 200, "pos_y": 1000},
    "ch01_0014": {"type": "narration", "speaker_id": null, "zh_cn": "凯点点头，退进雨里。", "en_us": "Kai nods and steps back into the rain.", "note": null, "pos_x": 400, "pos_y": 1000},
    "ch01_0015": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "屋里很乱，别介意。", "en_us": "The place is a mess. Don't mind it.", "note": null, "pos_x": 0, "pos_y": 1100},
    "ch01_0016": {"type": "narration", "speaker_id": null, "zh_cn": "你跟着她上了楼。", "en_us": "You follow her upstairs.", "note": null, "pos_x": 0, "pos_y": 1200},
    "ch01_0017": {"type": "narration", "speaker_id": null, "zh_cn": "客厅里堆着没拆的纸箱。", "en_us": "Unopened boxes are piled in the living room.", "note": "第二场：屋内", "pos_x": 0, "pos_y": 1300},
    "ch01_0018": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "我搬来三个月了，一直没收拾。", "en_us": "I moved in three months ago and still haven't unpacked.", "note": null, "pos_x": 0, "pos_y": 1400},
    "ch01_0019": {"type": "choice", "speaker_id": null, "zh_cn": "你想问什么？", "en_us": "What do you want to ask?", "note": "问什么", "pos_x": 0, "pos_y": 1500},
    "ch01_0020": {"type": "narration", "speaker_id": null, "zh_cn": "（问起她为什么搬走）", "en_us": "(Ask why she moved away)", "note": "选项 A", "pos_x": 200, "pos_y": 1600},
    "ch01_0021": {"type": "narration", "speaker_id": null, "zh_cn": "（什么都不问，帮她收拾）", "en_us": "(Ask nothing, help her tidy up)", "note": "选项 B", "pos_x": 400, "pos_y": 1600},
    "ch01_0022": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "我爸走后，这房子就空了。", "en_us": "After my dad passed, this place just sat empty.", "note": null, "pos_x": 200, "pos_y": 1700},
    "ch01_0023": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "我回来，是想把它卖掉。", "en_us": "I came back to sell it.", "note": null, "pos_x": 200, "pos_y": 1800},
    "ch01_0024": {"type": "choice", "speaker_id": null, "zh_cn": "你怎么回应？", "en_us": "How do you respond?", "note": "怎么回应", "pos_x": 200, "pos_y": 1900},
    "ch01_0025": {"type": "narration", "speaker_id": null, "zh_cn": "（说：别卖）", "en_us": "(Say: don't sell it)", "note": "选项 A", "pos_x": 100, "pos_y": 2000},
    "ch01_0026": {"type": "narration", "speaker_id": null, "zh_cn": "（什么也没说）", "en_us": "(Say nothing)", "note": "选项 B", "pos_x": 300, "pos_y": 2000},
    "ch01_0027": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "（怔了一下）你说这话，算什么。", "en_us": "(Taken aback) What are you even saying?", "note": null, "pos_x": 100, "pos_y": 2100},
    "ch01_0028": {"type": "narration", "speaker_id": null, "zh_cn": "雨声忽然大了。", "en_us": "The rain suddenly grows louder.", "note": null, "pos_x": 100, "pos_y": 2200},
    "ch01_0029": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "算了，先喝点热的吧。", "en_us": "Forget it. Let's have something hot first.", "note": null, "pos_x": 100, "pos_y": 2300},
    "ch01_0030": {"type": "line", "speaker_id": "spk_001_lin", "zh_cn": "谢谢你还来。", "en_us": "Thank you for coming.", "note": null, "pos_x": 0, "pos_y": 2400},
    "ch01_0031": {"type": "narration", "speaker_id": null, "zh_cn": "窗外，雨小了。", "en_us": "Outside, the rain has eased.", "note": null, "pos_x": 0, "pos_y": 2500},
    "ch01_0032": {"type": "end", "speaker_id": null, "zh_cn": "雨停了。", "en_us": "The rain has stopped.", "note": "章末", "pos_x": 0, "pos_y": 2600}
  },
  "edges": {
    "ch01_0001": [{"to_node": "ch01_0002", "seq": 0, "once": false}],
    "ch01_0002": [{"to_node": "ch01_0003", "seq": 0, "once": false}],
    "ch01_0003": [{"to_node": "ch01_0004", "seq": 0, "once": false}],
    "ch01_0004": [{"to_node": "ch01_0005", "seq": 0, "once": false}],
    "ch01_0005": [{"to_node": "ch01_0006", "seq": 0, "once": false}, {"to_node": "ch01_0007", "seq": 1, "once": true}],
    "ch01_0006": [{"to_node": "ch01_0008", "seq": 0, "once": false}],
    "ch01_0007": [{"to_node": "ch01_0008", "seq": 0, "once": false}],
    "ch01_0008": [{"to_node": "ch01_0009", "seq": 0, "once": false}],
    "ch01_0009": [{"to_node": "ch01_0010", "seq": 0, "once": false}],
    "ch01_0010": [{"to_node": "ch01_0011", "seq": 0, "once": false}, {"to_node": "ch01_0012", "seq": 1, "once": false}],
    "ch01_0011": [{"to_node": "ch01_0013", "seq": 0, "once": false}],
    "ch01_0012": [{"to_node": "ch01_0014", "seq": 0, "once": false}],
    "ch01_0013": [{"to_node": "ch01_0015", "seq": 0, "once": false}],
    "ch01_0014": [{"to_node": "ch01_0015", "seq": 0, "once": false}],
    "ch01_0015": [{"to_node": "ch01_0016", "seq": 0, "once": false}],
    "ch01_0016": [{"to_node": "ch01_0017", "seq": 0, "once": false}],
    "ch01_0017": [{"to_node": "ch01_0018", "seq": 0, "once": false}],
    "ch01_0018": [{"to_node": "ch01_0019", "seq": 0, "once": false}],
    "ch01_0019": [{"to_node": "ch01_0020", "seq": 0, "once": false}, {"to_node": "ch01_0021", "seq": 1, "once": false}],
    "ch01_0020": [{"to_node": "ch01_0022", "seq": 0, "once": false}],
    "ch01_0021": [{"to_node": "ch01_0030", "seq": 0, "once": false}],
    "ch01_0022": [{"to_node": "ch01_0023", "seq": 0, "once": false}],
    "ch01_0023": [{"to_node": "ch01_0024", "seq": 0, "once": false}],
    "ch01_0024": [{"to_node": "ch01_0025", "seq": 0, "once": false}, {"to_node": "ch01_0026", "seq": 1, "once": false}],
    "ch01_0025": [{"to_node": "ch01_0027", "seq": 0, "once": false}],
    "ch01_0026": [{"to_node": "ch01_0029", "seq": 0, "once": false}],
    "ch01_0027": [{"to_node": "ch01_0028", "seq": 0, "once": false}],
    "ch01_0028": [{"to_node": "ch01_0029", "seq": 0, "once": false}],
    "ch01_0029": [{"to_node": "ch01_0030", "seq": 0, "once": false}],
    "ch01_0030": [{"to_node": "ch01_0031", "seq": 0, "once": false}],
    "ch01_0031": [{"to_node": "ch01_0032", "seq": 0, "once": false}],
    "ch01_0032": []
  },
  "scenes": {
    "ch01_s01": {"entry_node": "ch01_0001", "bgm_default": "bgm_rain"},
    "ch01_s02": {"entry_node": "ch01_0017", "bgm_default": "bgm_room"}
  }
}
```

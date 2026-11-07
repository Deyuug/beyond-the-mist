# Beyond the Mist — ARR Hackathon 2026 Unity prototype

**One light, one boat, one shared discovery.** A two-screen experience for children during nebuliser treatment, built in 48 hours at the [ARR Hackathon](https://www.scivr.co.uk/arr-hackathon) (Asymmetric Reframed Reality, Goldsmiths SEE VR Lab with King's College London, 4–6 November 2026).

- **The child** captains a small sailing boat through a misty night sea on a big screen, steering with a one-hand joystick.
- **The parent** sees a different view, a treasure chart on a tablet, and turns a lighthouse beam to show the way. They can show the way, but only the child can steer the ship.
- **Breathing** calmly, in rhythm with a soft ring in the middle of the screen, clears the mist step by step. Breathing harder or longer never helps. There are no scores.
- **Clinicians** start, pause and end the session. The game never extends the treatment.

Demo video: [`docs/BeyondTheMist_demo.mp4`](docs/BeyondTheMist_demo.mp4) (75 s, English captions). Downloads for macOS and Windows are on the [Releases](../../releases) page. Unity 6000.3.28f1; everything except the background music is generated from code.

**Team:** Chengyan Tong (team lead, Unity build), Ruohao Sun, [Yutong Deng](https://github.com/Deyuug) and Junyi Wang. The idea and direction came from the whole team; the Unity prototype was built by Chengyan Tong, almost entirely with Claude Code as pair programmer, iterating on teammates' playtest feedback. The breathing in the demo is simulated: the prototype hears breathing through a microphone (sound, not airflow), so it is a non-medical demonstration only.

The rest of this README is in Chinese.

---

# 穿雾同行 Beyond the Mist — ARR Hackathon Unity Demo

**一束光、一艘船、一次共同发现。** 儿童在雾化治疗期间面对大屏幕，用单手摇杆驾驶一艘小帆船穿过夜晚的雾海。家长在平板上看到孩子看不到的隐藏惊喜，转动灯塔的光为孩子引路。孩子跟着画面中央的呼吸准星平稳地呼吸，海雾就会慢慢散开；多呼、用力呼都不会让雾散得更快。界面语言为英文。

> The parent can show the way, but only the child can steer the ship. 家长可以照亮前路，但只有孩子能够掌舵。

![航行中：左为孩子的大屏幕，右为家长的藏宝海图](docs/screenshots/03_revealing_0.png)

## 演示视频

[`docs/BeyondTheMist_demo.mp4`](docs/BeyondTheMist_demo.mp4)：75 秒，1080p，英文字幕。声音是舒缓的背景音乐，下面轻轻垫着游戏里柔和的海浪声；找到小海龟和珊瑚岛时各有一段轻柔的八音盒音效。

> 背景音乐："Silver Blue Light" by Kevin MacLeod (incompetech.com)，Licensed under Creative Commons: By Attribution 4.0 License（http://creativecommons.org/licenses/by/4.0/）。公开发布视频时请在说明里附上这行署名（见 `Tools/music/CREDITS.txt`）。内容依次是：三个角色、跟着呼吸准星平稳呼吸（星光随呼吸吸入、推开），浓雾一档一档散开、家长转灯和孩子掌舵、发现小海龟并停船、医护暂停、更多发现、医护结束。

重新录制（画面 + 游戏声音），再混入背景音乐：

```bash
"Builds/BeyondTheMist.app/Contents/MacOS/Beyond the Mist" -arrPromo /tmp/arr_promo
python3 Tools/make_promo.py /tmp/arr_promo /tmp/demo_nomusic.mp4
python3 Tools/swap_audio.py /tmp/demo_nomusic.mp4 /tmp/arr_promo/audio.wav "Tools/music/Silver Blue Light.mp3" docs/BeyondTheMist_demo.mp4
```

只换声音、不动画面时，用 `-arrAudioOnly` 在同一条时间线上只重新渲染游戏声音（几十秒），再用 `swap_audio.py` 替换音轨（画面直接复制，不重新编码）。

## 运行

- **macOS：** 打开 `Builds/BeyondTheMist.app`（同时支持 Intel 和 Apple 芯片）。
- **Windows：** 运行 `Builds/Windows/BeyondTheMist.exe`（64 位）。
- **Unity：** 用 Unity Hub 添加本文件夹（编辑器版本 6000.3.28f1），打开 `Assets/Scenes/BeyondTheMist.unity` 后点 Play。除背景音乐外，所有内容都由代码生成。
- **重新构建：** 菜单 `ARR → Build macOS App` 或 `ARR → Build Windows App`。

默认是分屏显示：左边是孩子的大屏幕，右边是家长平板（底部是独立的医护控制台）。按 `F1` 可以切换成单边全屏，推荐把孩子的画面全屏投到大屏幕上。接了两块屏幕时，按 `F2` 或用 `-dual` 参数启动。

## 一次完整的体验

1. **准备阶段：** 小船停在灯塔南边，船头正对着灯塔，金色的光从第一秒起就在画面里。
2. **医护开始：** 医护点「开始」，小船开始缓慢地自动前进。
3. **家长转动灯光：** 家长在海图上拖动（或用 `Q`/`E`），把灯光对准一个隐藏惊喜（小海龟、星光水母、小宝箱、珊瑚岛）。
4. **孩子跟光掌舵：** 孩子用摇杆把船转向金色光路。光束、光晕和金色飞鸟都能穿透雾，海面上的光路在雾里也会发亮。方向线索主要来自家长的灯光。只有在刚要前往新目的地、船头偏离光路超过 35° 时，孩子画面才会短暂出现一个指向光路的箭头，回到航线约 1 秒后消失；正常航行途中不显示。
5. **合作推进：** 只有灯光对准了惊喜、并且船沿着灯光航行时，目标周围的雾才会慢慢散开，惊喜从海里浮现出来。灯光对准的惊喜所在的一小片海面（直径约 20 米）会发出带波纹的柔和光，上方的雾里有一团暖金色光晕。光晕按视线方向计算，浓雾里也不会被近处的雾层挡住，跟着灯光走到附近就能看到目的地。船越近越亮（40 米内最亮），遇见后约 3 秒淡出；家长换目标时，先在原处淡出，再在新位置亮起。
6. **找到后停船：** 小船会慢慢停下，船头转到惊喜旁边，让孩子能好好看看它。家长把灯光转向下一个惊喜后，小船会再次出发。
7. **随时可以暂停和结束：** 医护随时可以暂停或结束。结束画面只写「今天遇见了……」，**不计数、不打分**。

## 家长平板

- **藏宝海图（左）：** 灯塔、小船和航迹、金色光束、四个隐藏的朋友（找到后标记 met）。在海图上按住拖动，光就转向手指的方向；下方有「Beam left / right」按钮和光束角度。
- **Teamwork：** 用文字描述两个人的配合：灯光是否照在惊喜上、孩子是否在跟着光、雾散到什么程度，以及一句现在可以做什么的提示。没有分数、进度条或百分比。
- **Breath · clearing the mist：** 呼吸波形，显示最近约 16 秒检测到的每一次呼吸（用麦克风或传感器时显示实时音量），下面是信号来源和「Simulate breath」按钮。
- **Hidden surprises：** 四个惊喜的状态（Hidden in mist / In the light / Mist clearing / Met）。
- **Your child is steering：** 孩子当前的航向和转向。
- **医护控制台（底部）：** 计划时长 3/5/8 分钟、计时、呼吸灯开关（Breath guide）、Start/Pause、End、Reset。
- 顶部还有 Demo speed 和 Switch view 两个演示按钮。

## 呼吸：帮助孩子形成舒适、稳定的节奏

目标不是"多呼气"，而是平静、稳定地呼吸。所以**雾按节奏一档一档地散开**：每一次跟上节奏的呼气把雾吹开一档（约 1/5），节奏一直对，雾就一次比一次淡，直到完全散开；节奏乱了，雾才慢慢回来，回来多少由匹配度决定。单次呼气不会把雾完全吹开，多呼、用力呼、呼得更长也不会让雾散得更快。

**呼吸准星（画面中央）**：没有文字，半透明，位于画面中央略偏上（船前方的海面，避开桅杆顶端），让孩子的视线自然落在画面中央。
- 细圆环随呼吸变大变小，位置固定不动，是一个稳定的注视点。
- 中间的小圆点随摇杆左右轻轻偏移，转向那一侧的小箭头会亮起，表示正在转向。
- 航行时约 60% 不透明；停船看朋友时降到约 25%，不遮挡朋友；暂停和结束画面上隐藏。
- **圆环变大 = 吸气：** 圆环张开、变成冷蓝色，周围的光点被吸进圆环。
- **圆环收缩 = 呼气：** 圆环变成金色并收缩，光点从圆钮向上流出。
- **停顿：** 小金环静止片刻，然后进入下一次吸气。

**引导先跟随孩子**（`BreathGuide.cs`）：
- 默认节奏是缓慢的每次 5 秒（约每分钟 12 次；吸 : 呼 : 停 ≈ 1.7 : 2.8 : 0.5）。开始后约 30 秒内，引导会向孩子自己测得的节奏靠拢（取最近几次呼吸间隔的中位数），但只在下面的范围内。
- 孩子呼气时，圆环的相位会往孩子的时机靠一点（通过短暂加快或放慢实现，不会跳变，也不会倒退），孩子不用去追圆环。如果孩子呼吸均匀、却连续两次都没落在节拍上（例如正好差了半拍），圆环也会重新对齐孩子。
- **圆环跟随孩子自己的节奏，从不要求孩子放慢：** 跟随范围是每次 2–6.5 秒（每分钟约 9–30 次），从幼儿平时的自然呼吸到较慢的平静呼吸都包括在内（demo 默认值，正式使用应由医护按孩子的年龄和情况设定）。开局约 30 秒内快速跟上孩子；之后孩子的节奏如果平稳地变了，圆环也会在几圈内跟过去；呼吸不均匀时圆环不去追。比孩子自己的节奏快很多的额外呼吸（短于约 55%）算作急促，不计入，圆环也不会跟着变快。孩子按自己的自然节奏平稳呼吸，就能把雾清掉。

**匹配度**（只看时间，不看声音大小、力度或呼气长短）：每一圈引导结束时评一次。
- 圆环开始收缩时有一次呼吸（从收缩前一点到收缩一半之间），并且离上一次呼吸约一个周期（±25%）→ 1
- 落在节拍上，但间隔不对（圆环会跟随孩子，碰巧对上很容易，间隔稳定才算节奏）→ 0.5
- 节拍上一次，外加一次平静的呼吸（呼吸稍快）→ 0.4
- 只有不在节拍上的呼吸（之后圆环会往孩子那边靠）→ 0.2
- 这一圈没检测到呼吸 → 0.2
- 离上一次太近的额外呼吸（急促）→ 0.1

每一圈的得分先取最近 3 圈的中位数：偶尔一次多呼或停顿几乎没有影响，连续两圈以上不跟节拍才会让匹配度下降。

**雾怎么散开、怎么回来**（"平静程度"，0 浓雾 … 1 全清）：
- **一档一档散开：** "好的呼吸"是落在节拍上、并且离上一次约一个周期。连续两次好的呼吸、而且两次间隔相近（相差不超过周期的 20%），第二次起每次让雾散开一档（0.2，约 0.5 秒内平滑完成）；第一次呼吸、或者长停顿后的第一次，单独就算。平稳跟上节奏时，大约 4–5 次呼吸雾完全散开；孩子按自己较快的自然节奏呼吸时，圆环先花十几秒跟上，之后同样一档一档散开。
- **防止碰巧对上：** 每一档最多只能比当前匹配度支撑的程度多 0.4，偶尔碰巧对上一次，散开得很少。
- **节奏保持时雾不回来：** 每次好的呼吸都让雾保持约 2.4 个周期；中间偶尔一次多呼或停顿，雾基本不动。
- **节奏丢了雾才回来，回来多少看匹配度：** 超过约 2.4 个周期没有好的呼吸，或者约 8 秒完全没检测到呼吸，雾以每秒 0.1 的速度慢慢回来（从全清到浓雾约 8–10 秒，不会突变），回到匹配度对应的程度。长时间平稳后偶尔乱一圈，几乎不回雾；连续几圈不跟节拍，回来一部分；呼吸不均匀，雾全部回来。

平静程度决定海面雾层、远处的距离雾和整屏的柔化：雾层浓度在 1.15 倍（浓雾）和 0.3 倍（全清）之间，远处的距离雾在 1.35 倍和 0.4 倍之间。浓雾时整个画面明显柔化（模糊加一层淡雾色，UI 不受影响），模糊强度和 11 月 5 日 18:07 那版一样；雾散开后画面基本清晰。船周围约 3–9 米内的雾稍淡一些（最多淡 45%），但不会清空。平静程度**不显示**在任何界面上：没有分数、没有"错了"的提示，孩子只看到海面越来越安静、清晰。
- **关闭引导时：** 呼吸间隔均匀（在每次 2–6.5 秒的范围内）就算好的呼吸，匹配度改为看呼吸是否均匀。均匀度用中位数计算，偶尔一次停顿或多呼几乎不影响。
- **没有检测到呼吸时**（约 8 秒以上，例如孩子停下来或声音太小麦克风听不到）：雾慢慢回到较浓的默认值，游戏照样能玩。

**每次呼吸的反馈**（`BreathFlow.cs` + `StardustSim.compute`）：每次呼吸都有同样的回应，让孩子感到呼吸被看见。
- **星光粒子随呼吸吸放：** 以小船为中心约 30 米内，呼气时漂浮的星光粒子带着拖影向外推开（约 2 秒），跟着圆环吸气时被吸向小船。只作用在星光粒子上，雾不受影响。
- **船头的小气流：** 船头送出一股很轻的向前气流（不可压缩的轴对称射流：轴向速度 `u_s = U·f(s)·g(ρ)`，径向速度 `u_ρ = −U·f′(s)·w²/(2ρ)·(1−g)`，满足 div u = 0），在船前把雾吹薄一条通道（最多薄一半，不会吹出空洞；不在节拍上的呼吸只有一半强度），随后慢慢合拢。
- 离上一次呼吸太近的呼吸没有任何额外反馈。

**模拟呼吸**（默认信号源）：模拟一个有自己节奏的孩子，节奏有自然波动，会逐渐跟上引导，偶尔多呼一次或停顿久一点，用来演示雾气随着呼吸平稳而慢慢散开。

**测试**（`-arrCapture <目录> -arrBreathTest`）：依次是跟随引导平稳呼吸 60 秒、完全不呼吸 30 秒、随意乱呼吸 30 秒、每 1.6 秒急促呼吸 30 秒、幼儿自然节奏（每 2.55–2.85 秒一次，约每分钟 22 次）平稳呼吸 50 秒、同样节奏但关闭引导 30 秒，平静程度写入 `breath_log.csv`。加 `-arrBreathNatural` 只测后两段，并且从开局就按自然节奏呼吸。最新实测：平稳呼吸每次跟上节奏升一档（0.2 → 0.4 → 0.6 → 0.8 → 1.0），约 20 秒全清，模拟孩子偶尔一次长停顿只让雾回来约 0.2；完全不呼吸约 8 秒后雾开始回来，约 10 秒回到浓雾；随意乱呼吸和急促呼吸一直是浓雾（0）；幼儿自然节奏从开局起约 16 秒圆环跟上、约 35 秒全清；关闭引导时约 15 秒全清。

**麦克风处理**（`BreathSignal.cs`，`B` 切换信号源）：
- **处理流程：** RMS → 开头 2 秒校准环境噪声（包括雾化器运转的声音）→ 归一化 → 滞回阈值加 1.5 秒冷却 → 记录一次呼吸的时间。之后只用这个时间，不用声音大小。
- **信号源：** 模拟自然呼吸（默认）、麦克风，或 UDP 9000 端口（接收 0–1 的数值）。
- **手动备用：** 空格键记录一次呼吸，作为现场演示的备用方案。

> **原型边界：** 麦克风只能测到声音的强弱，测不到真实的呼吸气流，戴面罩时判断呼吸的时机也不准。正式使用需要胸带或雾化管路里的气流/压力传感器（可通过 UDP 接入）。这里只是非医疗用途的演示信号。

## 海雾（参考 UE 的建议，用 Unity 实现）

| 建议 | 本项目中的实现 |
|---|---|
| Exponential Height Fog + Volumetric Fog | 8 层半透明雾切片（`MistLayers` + `Mist.shader`），越贴近海面越浓（指数高度分布）。灯塔光束穿过雾时会把雾照成金色，相当于体积光散射。全局雾保持较淡，让局部效果看得清楚 |
| Local Fog Volumes | 隐藏惊喜周围的雾更浓，随合作进度慢慢散开；灯塔岛和已经遇见的惊喜周围是清晰的空间 |
| 动态噪声雾材质 | 两个尺度的可平铺噪声缓慢流动，呈现连续、柔和的海雾，而不是一颗颗粒子组成的"烟" |
| Sphere Mask / 呼气推开 | 改成每次呼吸一条很小的向前射流通道加平流，只作用在船头前方；整体的雾由平静程度控制 |
| 逐渐恢复 | 节奏丢了雾才回来，回来多少由匹配度决定，从全清到浓雾约 8–10 秒，没有突变 |
| 实时性能和未来的 XR | 雾切片是固定数量的平面，开销可控；所有效果都在同一个场景里，没有新建场景 |

[`docs/breath_airflow.mp4`](docs/breath_airflow.mp4) 是一段 30 秒的录屏：模拟的孩子跟着画面中央的呼吸准星平稳呼吸，雾随每次跟上节奏的呼吸一档一档散开。

## 声音

游戏里是舒缓的背景音乐加上安静、柔和的环境声，没有像风一样的嘶嘶声：
- 背景音乐："Silver Blue Light"（Kevin MacLeod，CC BY 4.0，见下方署名），循环播放，开场 3 秒淡入，比海浪声高约 9 dB，流式加载。
- 温暖的底噪：粉红噪声经过两次约 450 Hz 的低通，不会发嘶。
- 海浪：每 6–10 秒一次，起落柔和；滤波器没有共振（共振扫频正是"风声"的来源），只打开到约 600 Hz，退去时带一点柔和的浪花声。
- 低沉的涌浪声。
- 每次呼吸：一阵约 300 Hz 的很轻的低频涌动，约 0.6 秒升起、1.4 秒落下。
- **找到朋友时**：一段轻柔的八音盒琶音（C 大调五声音阶，5 个音、间隔 0.18 秒，余音约 2.8 秒）。它只奖励家长和孩子一起完成的发现；呼吸不会触发任何奖励音效。背景音乐也是 C 大调，所以两者不会冲突。

除音乐外全部实时合成，`M` 一起静音。音乐署名写在游戏的快捷键帮助（`H`）里，Windows 包里也附有 `CREDITS.txt`。实测频谱：1 kHz 以上的能量约 2%，2 kHz 以上约 0.3%。

## 美术风格（参照「雾中航行」参考图）

| 参考图中的特点 | 实现方式 |
|---|---|
| 深海军蓝为底，暖金色是唯一的强调色 | 天空、海面、雾整体压到深蓝色调；灯塔光、帆、灯笼、惊喜的光晕用金色和暖色 |
| 印刷颗粒 / 点彩质感 | 孩子画面的后期效果 `StylizeFX` + `Stylize.shader`：暗部偏海军蓝、亮部偏暖金的分离色调，每秒变化约 12 次的细颗粒，再加一圈柔和暗角（界面文字不受影响） |
| 点彩月亮和星空 | 大号的点彩月亮，天边一抹淡淡的紫粉色渐变（参考 Monument Valley 的配色） |
| 平面感的黄色光束，里面有飞鸟 | 灯塔光改成边缘清晰的扁平光楔；一群金色的鸟沿着光束向外飞，会跟着光束一起转动，也能帮孩子找到光的方向 |
| 层叠蓬松的云海 | 雾的轮廓更清晰，朝向月亮的一侧有亮边，被灯光照到的地方是金色的亮边，色调有轻微的色阶分层 |
| 简洁的海面线条 | 海面上有几道随浪缓慢移动的细浅色流线 |

## 不打分

- **孩子端：** 没有按钮、没有计数、没有星星收集栏。结束画面只写「今天遇见了：小海龟、星光水母……」，作为一段回忆。
- **家长端：** 没有进度条和百分比，只用文字描述状态：「还藏在雾里」「雾在慢慢变淡」「就快出现了」「已经遇见」。呼吸波形只显示检测到的呼吸，不打分。
- **医护端：** 计时只给医护看，到了计划时长只做提示，不会自动结束。

## 操作

| 角色 | 操作 |
|---|---|
| 孩子 | 单手摇杆（手柄左摇杆）/ `←` `→` / `A` `D`：左右转向。停船时也能慢慢转头看四周 |
| 家长 | 在海图上拖动、按住「Turn light left / right」，或按 `Q` / `E`：转动灯塔光束 |
| 医护 | 操作栏：时长（开始前可切换 3/5/8 分钟）、呼吸灯开关、Start/Pause、End；键盘 `Enter` 开始 · `P` 暂停/继续 · `X` 结束 · `R` 重置 |
| 呼吸（演示用） | `空格` 记录一次呼吸；`B` 切换信号源；`G` 开关呼吸灯 |
| 演示 | `F1` 切换视图 · `F2` 双屏 · `]` 4 倍速 · `M` 静音 · `H` 快捷键帮助 |

## 分享给朋友

打包好的应用放在 GitHub 的 Releases 页面：
- **Windows：** 解压后运行 `BeyondTheMist.exe`。如果出现 SmartScreen 提示，点「更多信息 → 仍要运行」。
- **macOS：** 应用没有经过苹果签名，第一次打开时会被拦截。先双击一次，再到「系统设置 → 隐私与安全性」里点「仍要打开」。需要最新版时用菜单 `ARR → Build macOS App` 重新构建。

## 代码结构

`Assets/ARR/Scripts/`

| 文件 | 作用 |
|---|---|
| `Voyage.cs` | 共享状态：船、灯光、隐藏惊喜、合作判定、停船、灯塔岛碰撞、医护控制 |
| `BreathSignal.cs` | 呼吸事件（模拟 / 麦克风 / UDP） |
| `BreathGuide.cs` | 呼吸灯（先跟随孩子的节奏，再轻轻对齐相位）、匹配度和按节奏一档一档变化的平静程度（驱动雾气） |
| `AmbientAudio.cs` | 合成的白噪音与海浪声 |
| `BreathFlow.cs` | 每次呼吸的一股轻柔射流（CPU 和 GPU 共用同一套流场公式） |
| `MistLayers.cs` + `Resources/ARR/Mist.shader` | 分层海雾：高度雾、局部雾体、气流平流、灯光散射 |
| `Stardust.cs` + `StardustSim.compute` + `Stardust.shader` | GPU 星河粒子（丝带流线、空气中的细尘、星点），随呼吸以小船为中心吸入/推开 |
| `Models.cs` | 程序化建模工具（旋转体、圆管、布料三角面等） |
| `OceanView.cs` · `Waves.cs` | 夜海、小帆船、跟随镜头 |
| `LighthouseView.cs` | 灯塔建模与家长控制的光束（三层，可穿透雾） |
| `Surprises.cs` | 四个惊喜的建模、浮现与动画 |
| `MistField.cs` | 远处的雾团（增加景深） |
| `StylizeFX.cs` + `Resources/ARR/Stylize.shader` | 孩子画面的后期效果：浓雾时的模糊和一层淡雾色（随雾一档一档散开）、分离色调、印刷颗粒、暗角 |
| `ChildUI.cs` / `ParentUI.cs` | 孩子界面（无按钮；画面中央的半透明呼吸准星；只在转向新目的地时出现的方向箭头）/ 家长平板：藏宝海图、Teamwork、呼吸波形、隐藏惊喜列表、孩子转向，底部医护控制台 |
| `DemoAutoplay.cs` | `-arrCapture <目录>`：自动走完整流程并截图；`-arrQuick` 只跑呼吸段；`-arrClip` 录制 30 秒跟随呼吸灯的片段；`-arrTreasureTest` 把船放在小海龟前 60 米并对准灯光，分别截浓雾和平静时的画面，检查宝藏光斑 |
| `PromoRecorder.cs` | `-arrPromo <目录>`：按分镜逐帧录制 75 秒演示视频，同时把游戏自己的声音同步渲染成 `audio.wav`；再用 `Tools/make_promo.py` 加英文字幕合成 |
| `DeckShots.cs` | `-arrDeckShots <目录>`：给 PPT 拍几张孩子画面的截图（开场看到灯塔、发现水母、平静海面、吸气时浓雾、呼气时雾散开） |

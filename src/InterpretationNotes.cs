using System;
using System.Collections.Generic;
namespace PaperDays {
 // Editorial reading guidance, kept separate from the provider's daily text and scores.
 public static class InterpretationNotes {
  static readonly Dictionary<string,string[]> notes=new Dictionary<string,string[]> {
   {"overall",new[]{
    "综合部分描述的是当天值得留意的主题，不等于所有事情都会朝同一方向发展。可以把原文中的提醒分成“已经发生的事实”和“尚未确认的担心”，先处理前者，不用为了符合运势而寻找征兆。\n\n如果原文同时提到机会和波动，可先选一件能够完成的小事，再为临时变化留出余地。晚上回看实际进展，判断哪些建议有用；没有对应情境的部分可以直接略过。",
    "The overall reading suggests themes to reflect on, rather than one outcome for every part of your day. Separate what has actually happened from what you are only worried might happen; deal with the facts first.\n\nIf the text mentions both opportunities and uncertainty, choose one manageable task and leave room for changes. Review what really happened later. You do not need to look for signs that make the reading come true.",
    "종합 운세는 하루를 돌아볼 주제이지 모든 일이 같은 방향으로 흘러간다는 뜻은 아닙니다. 실제로 일어난 사실과 아직 확인되지 않은 걱정을 구분하고, 사실부터 살펴보세요.\n\n기회와 변동이 함께 언급된다면 끝낼 수 있는 작은 일 하나를 정하고 변경에 대비할 여유를 두세요. 하루 뒤 실제 결과를 돌아보고, 자신에게 해당하지 않는 내용은 넘겨도 됩니다.",
    "総合運は振り返るテーマであり、一日のすべての結果を決めるものではありません。実際に起きた事実と、まだ確かめていない心配を分けて考えてみましょう。\n\n好機と変化の両方が書かれている場合は、終えられる小さな課題を一つ選び、変更の余地を残します。後から実際の経過を振り返り、当てはまらない内容は読み流して構いません。"}},
   {"work",new[]{
    "工作方面可以重点看清楚任务、协作和节奏，而不是把一句“顺利”理解为无需准备。把当前任务的完成标准、负责人和截止时间写清楚；需要他人配合时，先确认双方对要求的理解一致。\n\n遇到意见不同，先讨论具体问题和可验证的结果，不急着判断对方态度。若原文提醒反复或变化，可先交付一个小版本并获得反馈，再继续投入；不要只凭运势评价自己的能力或决定去留。",
    "For work, focus on clear tasks, coordination and pace. A positive reading does not replace preparation: confirm what a finished task looks like, who owns it and when it is due.\n\nWhen opinions differ, discuss the concrete issue and a result you can check. If the reading mentions changes or delays, a small first draft can invite feedback before more effort is spent. Do not judge your ability or make career decisions from the reading alone.",
    "업무에서는 과제와 협업, 진행 속도를 구체적으로 확인하세요. 좋은 운세가 준비를 대신하지는 않습니다. 완료 기준과 담당자, 기한을 정하고 요청 내용을 서로 같은 뜻으로 이해했는지 확인하세요.\n\n의견이 다를 때는 상대의 태도를 단정하기보다 확인 가능한 결과를 논의하세요. 변화나 지연이 언급되면 작은 초안을 먼저 공유해 피드백을 받는 방법이 있습니다. 운세만으로 능력이나 진로를 판단하지 마세요.",
    "仕事では課題、連携、進め方を具体的に確認します。良い運勢でも準備は必要です。完了の基準、担当者、期限を整理し、依頼の意味が双方で一致しているか確かめましょう。\n\n意見が違う場合は相手の態度を決めつけず、確認できる成果について話します。変化や遅れが示されているなら、小さな案を先に共有して反応を得る方法があります。運勢だけで能力や進路を判断しないでください。"}},
   {"love",new[]{
    "感情与人际部分更适合用来检查沟通方式，不能据此判断某个人一定喜欢、讨厌或隐瞒什么。可以分别看“对方明确说过什么”“自己如何理解”和“还有什么需要询问”，避免把猜测当作对方的意思。\n\n想表达需要时，可说明一件具体的事情以及自己的感受，再给对方回应的空间。单身者不必因为桃花描述而强行推进关系；已有伴侣或正在合作的人，也可以先确认日常安排，减少信息不一致造成的误会。",
    "Use relationship themes to reflect on communication, not to infer another person's hidden feelings. Distinguish what they actually said, your interpretation and what you still need to ask.\n\nDescribe a specific situation and how you felt, then leave room for a reply. A romantic reading is not a reason to rush a relationship. Existing partners and friends can also reduce misunderstandings by checking everyday expectations.",
    "감정과 대인관계 내용은 소통을 돌아보는 참고로 읽고, 상대의 숨은 마음을 단정하는 근거로 삼지 마세요. 상대가 실제로 한 말, 자신의 해석, 더 물어봐야 할 점을 나누어 보세요.\n\n구체적인 상황과 느낀 점을 말한 뒤 답할 여유를 주세요. 연애운 때문에 관계를 서두를 필요는 없습니다. 가까운 사이에서도 일상적인 기대와 약속을 확인하면 오해를 줄이는 데 도움이 됩니다.",
    "恋愛や対人面は伝え方を見直す参考にし、相手の本心を断定する材料にはしません。実際に言われたこと、自分の解釈、確認したいことを分けて考えましょう。\n\n具体的な出来事と気持ちを伝えたら、相手が答える余地を残します。恋愛運を理由に関係を急ぐ必要はありません。親しい間柄でも日常の予定や期待を確認すると、行き違いに気づきやすくなります。"}},
   {"money",new[]{
    "财运文字中的“起伏”或“机会”并不是收入、收益或损失的可靠预告。阅读时可以把注意力放回自己知道的事实：是否存在临时消费、忘记的付款事项，或尚未核对清楚的条件，而不是寻找所谓幸运时机。\n\n原文如果提到冲动或信息不全，可把想做的决定先记下来，并另行核实真实资料。这里不提供投资、借贷或购买建议，也不应根据评分和幸运数字作金钱决定。",
    "Words such as opportunity or fluctuation are not reliable forecasts of income or losses. Bring attention back to facts you can check, such as an unplanned expense, a forgotten payment or unclear terms, rather than a lucky moment.\n\nIf the source mentions impulsiveness or missing information, write down the decision you are considering and verify the real details separately. This reading does not provide investment, borrowing or purchase advice.",
    "재물운의 기회나 변동이라는 표현은 수입이나 손실을 예고하는 근거가 아닙니다. 행운의 시점보다 계획 밖 지출, 잊은 결제, 확인되지 않은 조건처럼 직접 확인할 수 있는 사실을 살펴보세요.\n\n충동이나 정보 부족이 언급되면 생각 중인 결정을 적고 실제 자료를 별도로 확인하세요. 이 내용은 투자·대출·구매 조언을 제공하지 않으며 점수나 행운의 숫자로 금전 결정을 내려서는 안 됩니다.",
    "金運の「好機」や「変動」は収入や損失の予告ではありません。幸運な時期を探すより、予定外の支出、忘れていた支払い、未確認の条件など、自分で確かめられる事実に目を向けましょう。\n\n衝動や情報不足が書かれている場合は、検討している判断を書き留め、実際の情報を別途確認します。投資、借入、購入を勧める内容ではなく、点数や幸運の数字で金銭判断を行わないでください。"}},
   {"health",new[]{
    "健康或状态的描述不能判断身体是否健康，也不能解释症状。这里的文字最多作为留意日常状态的提醒；“状态不错”并不意味着可以忽视实际不适，“运势偏低”也不代表会生病。\n\n阅读时可以回顾今天是否安排过满、休息是否被事务挤占，再按自己的真实感受调整日程。涉及症状、用药或就医的问题，应依据实际情况和专业意见，而不是运势内容。",
    "Health or energy wording cannot assess your health or explain symptoms. A positive reading is not a reason to ignore discomfort, and a low score does not mean you will become ill.\n\nYou can use it as a prompt to review whether your day is overfilled or your breaks have been crowded out. Questions about symptoms, medication or care need real information and appropriate professional advice, not a horoscope.",
    "건강이나 컨디션 표현으로 몸 상태를 판단하거나 증상을 설명할 수는 없습니다. 좋은 운세라고 불편함을 무시해서는 안 되며 낮은 점수가 질병을 뜻하지도 않습니다.\n\n일정이 지나치게 빽빽한지, 쉴 시간이 밀려났는지 돌아보는 정도로 참고하세요. 증상이나 약, 진료와 관련된 문제는 운세가 아니라 실제 상태와 전문적인 의견을 바탕으로 판단해야 합니다.",
    "健康や調子についての表現は、体の状態や症状を判断するものではありません。良い運勢でも不調を無視せず、低い点数でも病気になるという意味には受け取らないでください。\n\n予定を詰め込みすぎていないか、休む時間が削られていないかを振り返るきっかけにはできます。症状、薬、受診については運勢ではなく、実際の状態と専門的な意見に基づいて考えます。"}},
   {"study",new[]{
    "学习方面可以把目标从“今天学得好不好”改成一个能够检查的小成果，例如独立完成一道题、说明一个概念，或整理一页错题。这样才能分清是真的理解，还是只觉得看过了。\n\n遇到卡住的地方，记录自己在哪一步失去思路，再带着具体问题查资料或询问。原文若提醒反复，可给复查留时间；若强调进展，也不必一次增加太多任务，先确认已有内容掌握稳固。",
    "For study, choose a result you can check: solve a problem independently, explain a concept or review a page of mistakes. This distinguishes understanding from simple familiarity.\n\nWhen stuck, record the step where your reasoning stopped and seek help with that specific question. Leave time for review if the reading mentions repetition; even an encouraging reading is not a reason to overload your plan.",
    "학습에서는 문제를 혼자 풀기, 개념 설명하기, 오답 한 쪽 정리하기처럼 확인할 수 있는 결과를 정하세요. 익숙하게 느끼는 것과 실제 이해를 구분할 수 있습니다.\n\n막혔다면 어느 단계에서 생각이 끊겼는지 적고 구체적으로 질문하세요. 반복이 언급되면 복습 시간을 남기고, 좋은 흐름이 언급되어도 과제를 무리하게 늘리지 마세요.",
    "学習では一問を自力で解く、概念を説明する、間違いを一ページ整理するなど、確認できる成果を決めましょう。見覚えがあるだけなのか、理解しているのかを分けられます。\n\n行き詰まったら、どの段階で考えが止まったかを記録して具体的に質問します。繰り返しへの注意があれば復習の時間を残し、良い流れでも課題を増やしすぎないようにします。"}},
   {"travel",new[]{
    "出行部分可以作为检查安排是否清楚的提示，不能用来预测路况或安全。先核对地点、日期、交通安排和需要携带的材料，把能够提前确认的事项处理好。\n\n若原文提到变化或等待，可以准备一个可替换的安排，并把临时变更及时告知同行者。真正出发前仍应查看实际天气、交通和官方通知；来源没有出行内容时，不把其他分类改写成出行预测。",
    "Travel themes can prompt a check of your arrangements, but cannot predict traffic or safety. Confirm the place, date, transport and required documents using actual information.\n\nIf the text mentions change or waiting, consider an alternative arrangement and tell companions about changes. Check real weather, transport and official notices before leaving. A missing travel forecast should not be filled in from unrelated categories.",
    "이동 관련 내용은 준비를 확인하는 참고일 뿐 교통이나 안전을 예측하지 못합니다. 장소와 날짜, 교통편, 필요한 자료를 실제 정보로 확인하세요.\n\n변경이나 대기가 언급되면 대체 일정을 생각하고 동행자에게 변경 사항을 알려주세요. 출발 전에는 실제 날씨와 교통, 공식 안내를 확인하세요. 출처에 없는 이동 운세를 다른 분류에서 만들어 넣지는 않습니다.",
    "外出についての内容は予定を確認するきっかけであり、交通や安全の予測ではありません。場所、日付、交通手段、必要な持ち物を実際の情報で確かめましょう。\n\n変更や待ち時間が書かれているなら代案を考え、同行者にも変更を伝えます。出発前は現実の天候、交通、公式情報を確認します。出典にない外出運を別の項目から作ることはしません。"}}
  };
  public static string Heading {get{return Pick(new[]{"解读与建议 · 应用补充","Interpretation and suggestions · App notes","해석과 제안 · 앱 보충","読み方と提案 · アプリ補足"});}}
  static string Pick(string[] values){int i=L.Language=="en"?1:L.Language=="ko"?2:L.Language=="ja"?3:0;return L.Language=="zh-TW"?L.Traditional(values[i]):values[i];}
  public static string For(string key){return Pick(notes[notes.ContainsKey(key)?key:"overall"]);}
  public static string ForCategory(string category){return For(category=="工作"?"work":category=="学习"?"study":category=="人际"?"love":category=="出行"?"travel":"overall");}
 }
}

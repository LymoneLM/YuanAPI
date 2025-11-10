# 贡献指北 Contributing

## 简要声明 Notice About Language

### English

Given the rapid development phase of the project and the limited availability of the main maintainers, the comments and documentation for this project will temporarily be written in Simplified Chinese.

However, this does not mean we are indifferent to international developers. We welcome any developers and players who are passionate about House of Legacy and mod development. We also accept contributions related to language support and documentation. Additionally, we recommend using AI tools to read the documentation and feel free to contact us.

### 简体中文

鉴于项目所处的快速开发阶段和主要维护者的精力问题，本项目注释以及文档，将暂时使用简体中文进行编写。

但这并不意味着我们不关心国际开发者，我们欢迎任何一个热爱吾今有世家和模组开发的开发者和玩家。也接受任何关于语言支持，文档编写的贡献。同时我们也推荐使用AI阅读文档，并随时联系我们交流。

## 快速开始

> **别说那么多没用的，我发现了问题，找到了修复方法，告诉我怎么为项目做贡献！！！**

如果你是这么想的话——那么只看这节就够了。

### 1. 提交一个Issue

在 `YuanAPI` 的 GitHub Issues 页面新建一个Issue，描述你发现的问题，或者你想要拓展的新功能。后续关于这个功能的讨论，大多数会在这里进行。

如果您只是想为我们贡献一个问题或者开发方向，您只需要持续跟进这个Issue就好了。如果您想完成开发，那么请明确在Issues里认领开发，并完成后续步骤。

### 2. 新建仓库分支

Fork `YuanAPI` 仓库（项目维护者可以直接在主仓库新建开发分支），并且我们推荐您在分叉仓库仍然新建开发分支进行开发。

### 3. 本地开发，提交commit

clone 您的Fork仓库到本地，进行代码编写。我们推荐您合理划分commit，并使用中文或者英文编写**有意义的提交信息**（可以参考主分支提交信息）。

### 4. 提交PR，关联到Issue

在GitHub提交Pull Request，将您的修改提交到**主仓库**的**主分支**，并将PR关联到此前提交的Issue，等待维护者进行检查和合并。

我们推荐您在提交PR之前先**rebase**合并目标分支的最新更改，后续在PR审核期间如果有冲突的更改，请使用**merge**处理合并。

> **项目进入稳定阶段后，我们可能会拒绝直接提交到主分支的PR，请提交到开发分支**

### 5. 后续跟进

大多数情况下，您只需要在PR被merge后按需清理分支或者仓库即可。

如果您提交了新的模块、新的功能或者重大修改，我们希望您在合并上线后的一段时间内，关注最新的Issue。观察是否存在与您的PR存在关联的问题，积极参与讨论并尝试解决。

至此，您已经完成了一次贡献，非常感谢您为模组社区和开源社区的付出！您的功绩将被永远记录在项目历史和贡献列表中！

## 项目哲学

> **元，始也，大也，至精之本也。立其根基，统其纲纪，通其变而易其成。**



## 项目结构



## 项目管理

为了规范项目管理（尤其是git版本管理），这一节会描述我们正在实践的管理方法，以及推荐的实践。我们推荐贡献者在参与贡献的过程中参考我们的实践，这将能优化版本管理规范性和有效性，并为PR提供快速检查通过的可能。

### 身份管理

### 代码管理

### 分支管理



### 提交管理

#### 实践

我们推荐**合理划分提交**，根据实际目的将代码修改包含到正确的提交中；同时单个提交应该尽可能专注于一个问题，不包含无关的代码和对不相关位置的影响。

请务必填写**有意义的提交信息**，可以描述提交的内容，而不是简单的Modify、Add或Changed。提交信息格式我们推荐参考[约定式提交](https://www.conventionalcommits.org/zh-hans/v1.0.0/)，但是这并不是强制要求。

约定式提交规范是一种基于提交信息的轻量级约定。 它提供了一组简单规则来创建清晰的提交历史； 这更有利于编写自动化工具。 通过在提交信息中描述功能、修复和破坏性变更。

#### 结构

提交说明的结构如下所示：

```
<类型>[可选 范围]: <描述>

[可选 正文]

[可选 脚注]
```

在 `YuanAPI` 的实践中，如果范围是根目录或者`src/`则可以**省略不写**。主项目中范围具体到**模块目录**即可，其余项目推荐包含**项目范围**即可。

#### 说明

提交说明包含了下面的结构化元素，以向类库使用者表明其意图：

1. **fix:** *类型* 为 `fix` 的提交表示在代码库中修复了一个 bug
2. **feat:** *类型* 为 `feat` 的提交表示在代码库中新增了一个功能
3. 除 `fix` 和 `feat` 之外，也可以使用其它提交 *类型*

   - **revert**: 用于回退提交，例如恢复某个文件在某几次提交之前的状态
   - **perf**: 用于优化性能，例如提升代码的性能、减少内存占用等
   - **refactor**: 用于重构代码，例如修改代码结构、变量名、函数名等但不修改功能逻辑
   - **docs**: 用于修改文档，例如修改 README 文件、API 文档等，单独修改代码注释也在此列
   - **style**: 用于修改代码的样式，例如调整缩进、空格、空行等
   - **test**: 用于修改测试用例，例如添加、删除、修改代码的测试用例等
   - **build**: 用于修改项目构建系统，例如修改依赖库、外部接口或者升级 Node 版本等
   - **chore**: 用于对非业务性代码进行修改，例如修改构建流程或者工具配置等，或者不属于以上条目的范围
4. **BREAKING CHANGE:** 在脚注中包含`BREAKING CHANGE:` 或 <类型>(范围) 后面有一个 `!` 的提交，表示引入了破坏性 API 变更
5. 脚注也可以用于关闭**GitHub Issues**，添加以下文字后，Issue将在commit被merge之后关闭:

   - close #xxx
   - closes #xxx
   - closed #xxx

#### 示例

```
fix(Core): 修复了子模块补丁错误的问题

修改子模块补丁范围，排除不必要的补丁

closed #12
```

# 贡献者公约 Code of Conduct

## Our Pledge

We pledge to make our community welcoming, safe, and equitable for all.

We are committed to fostering an environment that respects and promotes the dignity, rights, and contributions of all individuals. The same privileges of participation are extended to everyone who participates in good faith and in accordance with this Covenant.


## Encouraged Behaviors

While acknowledging differences in social norms, we all strive to meet our community's expectations for positive behavior. We also understand that our words and actions may be interpreted differently than we intend based on culture, background, or native language.

With these considerations in mind, we agree to behave mindfully toward each other and act in ways that center our shared values, including:

1. Respecting the **purpose of our community**, our activities, and our ways of gathering.
2. Engaging **kindly and honestly** with others.
3. Respecting **different viewpoints** and experiences.
4. **Taking responsibility** for our actions and contributions.
5. Gracefully giving and accepting **constructive feedback**.
6. Committing to **repairing harm** when it occurs.
7. Behaving in other ways that promote and sustain the **well-being of our community**.


## Restricted Behaviors

We agree to restrict the following behaviors in our community. Instances, threats, and promotion of these behaviors are violations of this Code of Conduct.

1. **Harassment.** Violating explicitly expressed boundaries or engaging in unnecessary personal attention after any clear request to stop.
2. **Character attacks.** Making insulting, demeaning, or pejorative comments directed at a community member or group of people.
3. **Stereotyping or discrimination.** Characterizing anyone’s personality or behavior on the basis of immutable identities or traits.
4. **Sexualization.** Behaving in a way that would generally be considered inappropriately intimate in the context or purpose of the community.
5. **Violating confidentiality**. Sharing or acting on someone's personal or private information without their permission.
6. **Endangerment.** Causing, encouraging, or threatening violence or other harm toward any person or group.
7. Behaving in other ways that **threaten the well-being** of our community.

### Other Restrictions

1. **Misleading identity.** Impersonating someone else for any reason, or pretending to be someone else to evade enforcement actions.
2. **Failing to credit sources.** Not properly crediting the sources of content you contribute.
3. **Promotional materials**. Sharing marketing or other commercial content in a way that is outside the norms of the community.
4. **Irresponsible communication.** Failing to responsibly present content which includes, links or describes any other restricted behaviors.


## Reporting an Issue

Tensions can occur between community members even when they are trying their best to collaborate. Not every conflict represents a code of conduct violation, and this Code of Conduct reinforces encouraged behaviors and norms that can help avoid conflicts and minimize harm.

When an incident does occur, it is important to report it promptly. To report a possible violation, you can directly contact the **Main Maintainers** and **Community Moderators** through any of our listed official communication channels, such as the QQ Group or Discord.

Community Moderators take reports of violations seriously and will make every effort to respond in a timely manner. They will investigate all reports of code of conduct violations, reviewing messages, logs, and recordings, or interviewing witnesses and other participants. Community Moderators will keep investigation and enforcement actions as transparent as possible while prioritizing safety and confidentiality. In order to honor these values, enforcement actions are carried out in private with the involved parties, but communicating to the whole community may be part of a mutually agreed upon resolution.


## Scope

This Code of Conduct applies within all community spaces, and also applies when an individual is officially representing the community in public or other spaces. Examples of representing our community include using an official email address, posting via an official social media account, or acting as an appointed representative at an online or offline event.


## Attribution

This Code of Conduct is adapted from the Contributor Covenant, version 3.0, permanently available at [https://www.contributor-covenant.org/version/3/0/](https://www.contributor-covenant.org/version/3/0/).

Contributor Covenant is stewarded by the Organization for Ethical Source and licensed under CC BY-SA 4.0. To view a copy of this license, visit [https://creativecommons.org/licenses/by-sa/4.0/](https://creativecommons.org/licenses/by-sa/4.0/)


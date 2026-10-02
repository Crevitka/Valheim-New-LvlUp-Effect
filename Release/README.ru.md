# New LvlUp Effect — 1.2.1

Автор: **Crevitka**. Лицензия: **MIT**.

Минималистичное уведомление о повышении навыка для Valheim: медальон, тонкий скандинавский орнамент, мягкое свечение и анимированная смена цифры. Например, «5» уходит вверх, а снизу на её место приходит «6».

Навыки и баланс не меняются. Штатные звук и эффекты сохраняются. Названия навыков берутся из локализации игры. Мод устанавливается только на клиент; серверу он не нужен. Jotunn не требуется.

![New LvlUp Effect в игре](https://raw.githubusercontent.com/Crevitka/Valheim-New-LvlUp-Effect/master/docs/levelup.gif)

**До / после:**

![Штатная надпись о навыке рядом с уведомлением New LvlUp Effect](https://raw.githubusercontent.com/Crevitka/Valheim-New-LvlUp-Effect/master/docs/before-after.jpg)

## Установка

Нужен BepInExPack Valheim 5.4.2202 или более новая совместимая сборка BepInEx 5.

Локальная сборка и журналы работы проверены с Valheim 1.0.15 и BepInExPack 5.4.2202; это не подтверждает совместимость со всеми сочетаниями модов.

- Thunderstore/r2modman: установи мод через менеджер.
- Архив Nexus: перенеси папку `BepInEx` в папку игры, где находится `valheim.exe`.
- Архив Thunderstore вручную: перенеси `plugins/NewLvlUpEffect` в `BepInEx/plugins`.

Итоговый путь: `BepInEx/plugins/NewLvlUpEffect/NewLvlUpEffect.dll`. Перезапусти игру. Устанавливай только одну копию DLL.

## Предпросмотр

Добавь параметр запуска `-console`, зайди в мир и введи в F5:

```text
newlvlupeffect_preview
```

Закрой F5: появится пример «Бег, 5 → 6». Навык не изменяется; `devcommands` для предпросмотра не нужен. Пока консоль открыта, анимация приостановлена.

## Настройки

Файл: `BepInEx/config/local.valheim.newlvlupeffect.cfg`. Создаётся при первом запуске. После редактирования перезапусти игру.

- `Enabled = true`: включить уведомления.
- `ReplaceVanillaMessage = true`: скрыть штатную экранную надпись, включая `raiseskill`; вывод внутри консоли остаётся.
- `Duration = 3.6`: длительность в секундах (1.5–8).
- `VerticalPosition = 0.77`: положение по высоте, считая снизу (0.2–0.9).
- `Scale = 1`: размер (0.6–1.5).

Одновременные повышения идут по очереди. Повторные ожидающие уведомления одного навыка объединяются. Очередь ограничена 32 записями. При `raiseskill` показывается реальный переход от старого уровня к новому, если целая часть уровня выросла.

## Совместимость

Другие моды уведомлений могут дублировать сообщения. Старые версии игры, Linux, Steam Deck и нестандартные системы навыков не проверены. Обычный вывод `raiseskill` сохранён.

Для диагностики смотри `BepInEx/LogOutput.log`: сообщения `Loading [New LvlUp Effect 1.2.1]`, `Queued raiseskill notification` и `Showing`. Перед отправкой журнала убери личные данные.

Для обновления закрой игру и замени DLL. Для удаления убери DLL или удали мод через менеджер. Настройки при обновлении сохраняются.

Разработано с помощью ИИ. Иконки и шрифт игры используются во время работы, но не включены в архив. Неофициальный мод для Valheim.

## Ссылки

- **GitHub:** [github.com/Crevitka/Valheim-New-LvlUp-Effect](https://github.com/Crevitka/Valheim-New-LvlUp-Effect) — исходники и баг-репорты
- **Discord:** [discord.gg/F2UehNhe96](https://discord.gg/F2UehNhe96) — поддержка и обратная связь
- **Reddit:** [reddit.com/user/Crevitka](https://www.reddit.com/user/Crevitka/)
- **X:** [x.com/Crevitka](https://x.com/Crevitka)

Вопросы, баг-репорты и идеи — в Discord или в issues на GitHub.

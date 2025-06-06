
‎
# Sync Folders
  
[Latest SyncFolders Sources and Packages](https://github.com/NataljaNeumann/SyncFolders)  
  
  
![SyncFolders-1-Arrow](https://github.com/user-attachments/assets/9a7ad087-7cd5-4ced-ae4d-be2445be90b9)  

‎[English](#en), [Français](#fr), [Español](#es), [Português](#pt), [Italiano](#it), [Deutsch](#de), [По русски](#ru), [Polski](#pl), [Στα ελληνικά](#gr), 
      [Nederlands](#nl), [Dansk](#da), [Suomeksi](#fi), [Svenska](#sv), [Türkçe](#tr), [中文文本](#chs), [中文文字](#cht), [日本語](#ja), [한국인](#ko), [भारतीय में](#hi), [باللغة العربية](#ar), [עִברִית](#he)
‎
# English
<a name="en"></a>
‎SyncFolders is an application that aims to help you to keep two folders or drives synchronised.
      With default settings this apllication creates hidden files, about 1% of the size of original files, that allow
      you to completely recover from single block failures. There are two layers of protection:
1. You keep two different drives with complete copies of files.
2. Even if a drive becomes inaccessible, e.g. power failure or other problems, single block failures
          and also bigger error ranges in files can be restored using additionally saved information.

‎The original files and additionally saved information can be verified by the application. In case
    of errors the appllication will try to repair the file. If there is a second copy with same date and
    same length, then app will try to recover single blocks from the other copy. If this fails then app
    will try to recover single blocks from additionally saved backup information in hidden files.

‎In case all mentioned measures fail, the application will also try to restore an older copy from the other
    directory. This is the standard behavior of backup applications: they restore old copies of same files.

‎After all, if nothing worked, then the application will recover the available parts of the file by overwriting
    unreadable blocks with zeros, so at least the file can be read and copied, even if not all parts of it are OK.
    Many media applications can jump over these missing parts.

‎All of this is done automatically, so your personal family photos and videos are kept safe as good as possible.

‎A log is shown after completion of the operation and also saved in Documents folder for later reference.

‎There is no need of an installation. You can extract the archive into a subfolder of the drive that contains
    photos and run it from there. Windows will eventually prompt you to install .NET-Framework, that is all you need.

‎If you choose to copy from first to second directory, then the app will treat the first directory as primary
    data source and second directory as backup. If the application discovers that a file in first directory contains
    bad blocks, the application still can try to restore the blocks from backup. The application also can restore old 
    version of the same file from the backup directory. You need to specify that first directory is not writable,
    so the application doesn't try to modify the files in first directory.

‎The application usually runs in synchronization mode, which means that it will try to copy the newest version
    of the photoes to the respective other folder or drive. If you remove the synchronization mode, then the application
    can also overwrite new files in second directory by old files in first directory.

‎If you specify the same directory or drive in the first and second directory (e.g. E:\ and E:\ ), 
    the application works in the same mode as SaveMyFiles - it creates additional data if it is missing, 
    or verifies and repairs the files if appropriate options were specified.

> [!NOTE]
> ‎If second directory contains special file, with the name specified below [^1], then the application
>     doesn't delete the files in it. In this case it ignores that you specified to delete the files in second directory.
  
[Do you need support?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Français
<a name="fr"></a>
‎SyncFolders est une application qui vise à vous aider à garder deux dossiers ou lecteurs synchronisés. Avec les 
    paramètres par défaut, cette application crée des fichiers cachés, environ 1 % de la taille des fichiers d'origine, 
    qui vous permettent de récupérer complètement après des pannes d'un seul bloc. Il existe deux niveaux de protection:
1. Vous conservez deux lecteurs différents avec des copies complètes des fichiers.
2. Même si un lecteur devient inaccessible, par ex. une panne de courant ou d'autres problèmes, des pannes 
      de bloc uniques ainsi que des plages d'erreurs plus importantes dans les fichiers peuvent être restaurées 
      à l'aide d'informations enregistrées supplémentaires.

‎Les fichiers originaux et les informations supplémentaires enregistrées peuvent être vérifiés par l'application. 
    En cas d'erreurs, l'application tentera de réparer le fichier. S'il existe une deuxième copie avec la même date et 
    la même longueur, l'application tentera de récupérer des blocs uniques de l'autre copie. Si cela échoue, 
    l'application tentera de récupérer des informations supplémentaires enregistrées.

‎En cas d'échec de toutes les mesures mentionnées, l'application tentera également de copier une copie plus ancienne 
    de l'autre lecteur, ce qui constitue le comportement standard des applications de sauvegarde.

‎Après tout, si rien ne fonctionne, l'application récupérera les parties disponibles du fichier en écrasant les 
    blocs illisibles par des zéros, afin qu'au moins le fichier puisse être lu et copié, même si toutes les parties 
    ne sont pas correctes. De nombreuses applications multimédias peuvent combler ces lacunes.

‎Tout cela se fait automatiquement, afin que vos photos et vidéos personnelles de famille soient protégées autant que possible.

‎Un journal est affiché une fois l'opération terminée et également enregistré dans le dossier Documents pour référence ultérieure.

‎l n'y a pas besoin d'installation. Vous pouvez extraire l'archive dans un sous-dossier du lecteur contenant des photos 
    et l'exécuter à partir de là. Windows vous demandera éventuellement d'installer .NET-Framework, c'est tout ce dont vous avez besoin.

‎Si vous choisissez de copier du premier vers le deuxième répertoire, l'application traitera le premier répertoire comme 
    source de données principale et le deuxième répertoire comme sauvegarde. Si l'application découvre qu'un fichier du premier 
    répertoire contient des blocs défectueux, elle peut toujours essayer de restaurer les blocs de la sauvegarde dans le 
    deuxième dossier, ou l'ancienne version du même fichier à partir de la sauvegarde dans le deuxième répertoire. 
    Vous devez spécifier que le premier répertoire n'est pas accessible en écriture, afin que 
    l'application n'essaye pas de modifier les fichiers du premier répertoire.

‎L'application fonctionne généralement en mode synchronisation, ce qui signifie qu'elle essaiera de copier 
    la version la plus récente des photos dans l'autre dossier ou lecteur correspondant. Si vous supprimez le mode 
    de synchronisation, l'application peut également écraser les nouveaux fichiers du deuxième répertoire par les 
    anciens fichiers du premier répertoire. L'application s'exécute généralement en mode de synchronisation, 
    ce qui signifie qu'elle essaiera de copier la version la plus récente des photos dans le répertoire respectif. 
    autre dossier ou lecteur. Si vous supprimez le mode de synchronisation, l'application peut également remplacer 
    les nouveaux fichiers du deuxième répertoire par les anciens fichiers du premier répertoire.

‎Si vous spécifiez le même répertoire ou lecteur dans le premier et le deuxième répertoire (par exemple E:\ et E:\ ), 
    l'application fonctionne dans le même mode que SaveMyFiles - elle crée des données supplémentaires si elles sont manquantes, 
    ou vérifie et répare les fichiers si les options appropriées ont été spécifiées.

> [!NOTE]
> ‎Si le deuxième répertoire contient un fichier spécial, dont le nom est spécifié ci-dessous [^1],
>     l'application ne supprime pas les fichiers qu'il contient lorsque vous spécifiez de supprimer les fichiers du deuxième répertoire.
  
[Avez-vous besoin de soutien?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wikia](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Español
<a name="es"></a>
‎SyncFolders es una aplicación que tiene como objetivo ayudarte a mantener sincronizadas dos carpetas o unidades.
    Con la configuración predeterminada, esta aplicación crea archivos ocultos, aproximadamente el 1% del tamaño de 
    los archivos originales, que permiten para recuperarse completamente de los errores de un solo bloque. 
    Hay dos capas de protección:
- Conserva dos unidades diferentes con copias completas de los archivos.
- Incluso si una unidad se vuelve inaccesible, por ejemplo, corte de energía u otros problemas, 
      fallas de un solo bloque y también se pueden restaurar rangos de error más grandes en los archivos
      utilizando información guardada adicionalmente.

‎La aplicación puede verificar los archivos originales y la información guardada adicionalmente. 
    Por si de errores, la aplicación intentará reparar el archivo. Si hay una segunda copia con la 
    misma fecha y misma longitud, luego la aplicación intentará recuperar bloques individuales de la 
    otra copia. Si esto falla, entonces la aplicación intentará recuperarse de la información guardada adicionalmente.

‎En caso de que todas las medidas mencionadas fallen, la aplicación también intentará copiar una 
    copia anterior de la otra unidad, que es el comportamiento estándar de las aplicaciones de copia de seguridad.

‎Después de todo, si nada funcionó, la aplicación recuperará las partes disponibles del archivo 
    sobrescribiendo bloques ilegibles con ceros, por lo que al menos el archivo se puede leer y copiar, 
    incluso si no todas las partes del mismo están bien. Muchas aplicaciones multimedia pueden saltar 
    por encima de estas partes faltantes.

‎Todo esto se hace automáticamente, por lo que sus fotos y videos familiares personales se mantienen seguros lo mejor posible.

‎Se muestra un registro después de completar la operación y también se guarda en la carpeta Documentos para referencia posterior.

‎No hay necesidad de instalación. Puede extraer el archivo en una subcarpeta de la unidad que contiene 
    fotos y ejecutarlo desde allí. Eventualmente, Windows le pedirá que instale .NET-Framework, eso es todo lo que necesitas.

‎Si elige copiar del primer directorio al segundo directorio, la aplicación tratará el primer directorio como la fuente 
    de datos principal y el segundo directorio como la copia de seguridad. Si la aplicación detecta que un archivo en el primer directorio 
    contiene bloques defectuosos, aún puede intentar restaurar los bloques desde la copia de seguridad en la segunda carpeta o la versión 
    anterior del mismo archivo desde la copia de seguridad en el segundo directorio. Debe especificar que no se puede escribir en el primer 
    directorio para que la aplicación no intente modificar los archivos del primer directorio.

‎La aplicación generalmente se ejecuta en modo de sincronización, lo que significa que intentará copiar 
    la versión más reciente de las fotos a la otra carpeta o unidad respectiva. Si quita el modo de sincronización, 
    la aplicación También puede sobrescribir nuevos archivos en el segundo directorio por archivos antiguos en el primer directorio.

‎Si especifica el mismo directorio o unidad en el primer y segundo directorio (por ejemplo, E:\ y E:\ ), 
    la aplicación funciona en el mismo modo que SaveMyFiles: crea datos adicionales si faltan, 
    o verifica y repara los archivos si se especificaron las opciones adecuadas.

> [!NOTE]
> ‎Si el segundo directorio contiene un archivo especial, con el nombre especificado a continuación [^1], 
>     entonces la aplicación No elimina los archivos que contiene, cuando se especifica eliminar los archivos en el segundo directorio.
  
[¿Necesitas apoyo?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Português
<a name="pt"></a>
‎SyncFolders es una aplicación diseñada para ayudarle a mantener dos carpetas o unidades sincronizadas. 
    Con la configuración predeterminada, esta aplicación crea archivos ocultos de aproximadamente el 1% del tamaño de los archivos originales 
    y le permite realizar una recuperación completa después de errores de bloqueo individuales. Hay dos niveles de protección:
1. Você mantém duas unidades diferentes com cópias completas dos arquivos.
2. Mesmo que uma unidade se torne inacessível, por exemplo, falha de energia ou outros problemas, falhas de bloco 
      único e também intervalos de erro maiores em arquivos podem ser restaurados usando informações salvas adicionalmente.

‎Os arquivos originais e as informações salvas adicionalmente podem ser verificados pelo aplicativo. Caso de erros, 
    o aplicativo tentará reparar o arquivo. Se houver uma segunda via com a mesma data e mesmo comprimento, 
    o aplicativo tentará recuperar blocos únicos da outra cópia. Se isso falhar, o aplicativo 
    tentará se recuperar de informações salvas adicionalmente.

‎Caso todas as medidas mencionadas falhem, o aplicativo também tentará copiar uma cópia mais antiga da outra 
    unidade, que é o comportamento padrão de aplicativos de backup.

‎Afinal, se nada funcionou, o aplicativo recuperará as partes disponíveis do arquivo substituindo blocos 
    ilegíveis com zeros, então pelo menos o arquivo pode ser lido e copiado, mesmo que nem todas as partes dele estejam OK. 
    Muitos aplicativos de mídia podem pular essas peças ausentes.

‎Tudo isso é feito automaticamente, para que suas fotos e vídeos pessoais de 
    família sejam mantidos seguros da melhor maneira possível.

‎Um log é mostrado após a conclusão da operação e também salvo na pasta Documentos para referência posterior.

‎Não há necessidade de instalação. Você pode extrair o arquivo em uma subpasta da unidade que contém fotos 
    e executá-lo a partir daí. O Windows eventualmente solicitará que você instale o .NET-Framework, isso é tudo que você precisa.

‎Se você optar por copiar do primeiro para o segundo diretório, o aplicativo tratará o primeiro diretório 
    como primário fonte de dados e segundo diretório como backup. Se o aplicativo descobrir que um arquivo no 
    primeiro diretório tem blocos defeituosos, ele ainda pode tentar restaurar os blocos do backup na segunda 
    pasta ou na versão mais antiga do mesmo arquivo do backup no segundo diretório. Você precisa especificar que 
    o primeiro diretório não é gravável, Portanto, o aplicativo não tenta modificar os arquivos no primeiro diretório.

‎O aplicativo geralmente é executado no modo de sincronização, o que significa que ele tentará copiar a 
    versão mais recente das fotos para a respectiva outra pasta ou unidade. Se você remover o modo de sincronização, 
    o aplicativo também pode substituir novos arquivos no segundo diretório por arquivos antigos no primeiro diretório.

‎Se especificar o mesmo diretório ou unidade no primeiro e no segundo diretório (por exemplo, E:\ e E:\ ), 
    a aplicação funcionará no mesmo modo que o SaveMyFiles: cria dados adicionais se estiverem em falta 
    ou verifica e repara os ficheiros se as opções apropriadas tiverem sido especificadas.

> [!NOTE]
> ‎Se o segundo diretório contiver um arquivo especial, com o nome especificado abaixo [^1], 
>     o aplicativo não exclui os arquivos nele, quando você especifica para excluir os arquivos no segundo diretório.
  
[Você precisa de suporte?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Italiano
<a name="it"></a>
‎SyncFolders è un'applicazione che ha lo scopo di aiutarti a mantenere sincronizzate due cartelle o unità. 
    Con le impostazioni predefinite questa applicazione crea file nascosti, circa l'1% della dimensione dei 
    file originali, che consentono per eseguire il ripristino completo da errori di blocco singolo. 
    Esistono due livelli di protezione:
1. Conservi due unità diverse con copie complete dei file.
2. Anche se un'unità diventa inaccessibile, ad esempio in caso di interruzione di corrente o 
      altri problemi, i guasti di un singolo blocco e anche intervalli di errore più grandi nei file 
      possono essere ripristinati utilizzando le informazioni salvate in aggiunta.

‎I file originali e le informazioni aggiuntive salvate possono essere verificati dall'applicazione. 
    Qualora di errori, l'applicazione tenterà di riparare il file. Se esiste una seconda copia con la 
    stessa data e stessa lunghezza, quindi l'app proverà a recuperare singoli blocchi dall'altra copia. 
    Se questo non riesce, l'app Tenterà di recuperare singoli blocchi da informazioni di backup salvate 
    in aggiunta nei file nascosti.

‎Nel caso in cui tutte le misure menzionate falliscano, l'applicazione proverà anche a ripristinare 
    una copia precedente dall'altra Directory. Questo è il comportamento standard delle applicazioni di backup: 
    ripristinano vecchie copie degli stessi file.

‎Se nulla ha funzionato, l'applicazione recupererà le parti disponibili del file 
    sovrascrivendo blocchi illeggibili con zeri, in modo che almeno il file possa essere letto e copiato, 
    anche se non tutte le parti di esso sono OK. Molte applicazioni multimediali possono saltare queste parti mancanti.

‎Tutto questo viene fatto automaticamente, quindi le tue foto e i tuoi video personali di famiglia sono tenuti al sicuro nel miglior modo possibile.

‎Al termine dell'operazione viene visualizzato un registro che viene salvato nella cartella Documenti per riferimento futuro.

‎Non è necessaria un'installazione. È possibile estrarre l'archivio in una sottocartella dell'unità 
    che contiene foto ed eseguirlo da lì. Windows chiederà di installare .NET-Framework, questo è tutto ciò di cui hai bisogno.

‎Se scegli di copiare dalla prima alla seconda directory, l'app considererà la prima directory come 
    primaria origine dati e seconda directory come backup. Se l'applicazione rileva che un file nella prima 
    directory contiene blocchi danneggiati, l'applicazione può ancora provare a ripristinare i blocchi dal backup. 
    L'applicazione può anche ripristinare il vecchio versione dello stesso file dalla directory di backup. 
    È necessario specificare che la prima directory non è scrivibile, Quindi l'applicazione non tenta 
    di modificare i file nella prima directory.

‎L'applicazione di solito viene eseguita in modalità di sincronizzazione, il che significa che 
    tenterà di copiare la versione più recente delle foto nella rispettiva altra cartella o unità. Se si 
    rimuove la modalità di sincronizzazione, l'applicazione può anche sovrascrivere i nuovi file 
    nella seconda directory con vecchi file nella prima directory.

‎Se si specifica la stessa directory o unità nella prima e nella seconda directory (ad esempio E:\ e E:\ ), 
    l'applicazione funziona nella stessa modalità di SaveMyFiles: crea dati aggiuntivi se mancano oppure 
    verifica e ripara i file se sono state specificate le opzioni appropriate.

> [!NOTE]
> ‎Se la seconda directory contiene un file speciale, con il nome specificato di seguito [^1], 
>     allora l'applicazione non elimina i file in esso contenuti. In questo caso, ignora che è stato 
>     specificato di eliminare i file nella seconda directory.
  
[Hai bisogno di supporto?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Deutsch
<a name="de"></a>
‎SyncFolders ist eine Anwendung, die Ihnen helfen soll, zwei Ordner oder Laufwerke synchron zu halten. 
    Mit den Standardeinstellungen erstellt diese Anwendung versteckte Dateien, die etwa 1% der Größe der 
    Originaldateien betragen und Sie können nach Fehlern einzelner Blöcke eine vollständige Wiederherstellung durchführen. 
    Es gibt zwei Schutzebenen:
1. Sie behalten zwei verschiedene Laufwerke mit vollständigen Kopien von Dateien.
2. Selbst wenn ein Laufwerk nicht mehr zugänglich ist, z. B. durch Stromausfall oder andere Probleme, 
      Ausfälle einzelner Blöcke und auch größere Fehlerbereiche in Dateien können mit zusätzlich 
      gespeicherten Informationen wiederhergestellt werden.

‎Die Originaldateien und zusätzlich gespeicherte Informationen können von der Anwendung überprüft werden. 
    Bei Fehlern versucht die Anwendung, die Datei zu reparieren. Wenn es eine zweite Kopie mit demselben 
    Datum und gleicher Länge gibt, dann versucht die App, einzelne Blöcke von der anderen Kopie wiederherzustellen. 
    Wenn dies fehlschlägt, wird die App versuchen, einzelne Blöcke aus zusätzlich gespeicherten 
    Backup-Informationen in versteckten Dateien wiederherzustellen.

‎Falls alle genannten Maßnahmen fehlschlagen, versucht die Anwendung auch, eine ältere Kopie
    vom anderen Laufwerk wiedeherzustellen. Das ist das Standardverhalten von Backup-Anwendungen.

‎Wenn nichts funktioniert hat, stellt die Anwendung die verfügbaren Teile der Datei durch 
    Überschreiben unlesbarer Blöcke mit Nullen wieder her, damit zumindest die Datei gelesen und kopiert 
    werden kann, auch wenn nicht alle Teile davon in Ordnung sind. Viele Medienanwendungen 
    können über diese fehlenden Teile hinwegspringen.

‎All dies geschieht automatisch, sodass Ihre persönlichen Familienfotos und -videos so sicher wie möglich aufbewahrt werden.

‎Nach Abschluss des Vorgangs wird ein Protokoll angezeigt und zur späteren Bezugnahme auch im Ordner "Dokumente" gespeichert.

‎Eine Installation ist nicht erforderlich. Sie können das Zip-Archiv in einen Unterordner des Laufwerks extrahieren, 
    der Photos enthält, und es von dort aus ausführen. Windows fordert Sie schließlich auf, .NET-Framework zu installieren, 
    das ist alles, was Sie brauchen.

‎Wenn Sie sich für das Kopieren vom ersten in das zweite Verzeichnis entscheiden, behandelt die 
    App das erste Verzeichnis als primäres Verzeichnis Datenquelle und zweites Verzeichnis als Backup. 
    Wenn die Anwendung feststellt, dass eine Datei im ersten Verzeichnis fehlerhafte Blöcke enthält, kann sie immer noch versuchen, 
    die Blöcke aus dem Backup im zweiten Ordner oder die ältere Version derselben Datei aus dem Backup im zweiten Verzeichnis 
    wiederherzustellenn. Sie müssen angeben, dass das erste Verzeichnis nicht beschreibbar ist, damit die Anwendung nicht verucht, 
    die Dateien im ersten Verzeichnis zu ändern.

‎Die Anwendung wird normalerweise im Synchronisationsmodus ausgeführt, was bedeutet, dass sie versucht, 
    die neueste Version der Fotos zu kopieren in den jeweils anderen Ordner oder das jeweils andere Laufwerk. 
    Wenn Sie den Synchronisationsmodus ausschalten, kann die Anwendung auch neue Dateien im zweiten Verzeichnis 
    durch alte Dateien im ersten Verzeichnis überschreiben.

‎Wenn man im ersten und zweiten Verzeichnis gleiches Verzeichnis bzw. Laufwerk angibt (z.B. E:\ und E:\ ), 
    dann arbeitet die Applikation im gleichen Modus wie SaveMyFiles - sie erstellt zusätzliche Daten, wenn diese fehlen, 
    bzw. verifiziert und repariert die Dateien, wenn entsprechende Optionen angegeben wurden.

> [!NOTE]
> ‎Wenn das zweite Verzeichnis eine spezielle Datei mit dem unten [^1] angegebenen Namen enthält,
>       dann wird die Anwendung die Dateien darin nicht löschen. In diesem Fall wird ignoriert, dass Sie angegeben haben, 
>       dass die Dateien im zweiten Verzeichnis gelöscht werden sollen.
  
[Brauchen Sie Unterstützung?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# По русски
<a name="ru"></a>
‎SyncFolders – это приложение, которое призвано помочь вам синхронизировать две папки или диска. 
    При настройках по умолчанию, это приложение создает скрытые файлы, примерно 1% от 
    размера исходных файлов, которые позволяют полностью восстановиться после сбоев отдельных блоков на диске. 
    Существует два уровня защиты:
1. Вы храните два разных диска с полными копиями файлов.
2. Даже если диск становится недоступным, например, из-за сбоя питания или других проблем,
        отдельные блоки файлов могут быть восстановлены, а также бóльшие диапазоны ошибок в файлах,
        при помощи дополнительной сохраненной информации.

‎Исходные файлы и сохраненная дополнительная информация могут быть проверены приложением. 
    В случае возникновения ошибок приложение попытается восстановить файл. Если есть второй экземпляр файла
    с той же датой и той же длинной, приложение будет пытаться извлечь отдельные блоки из другой копии. 
    Если это не удастся, приложение попытается подключить дополнительную сохраненную информацию.

‎Если все перечисленные меры не увенчаются успехом, приложение также попытается скопировать 
    более старую копию на другом диске, что является стандартным поведением приложений резервного копирования.

‎В конце концов, если ничего не работает, приложение восстановит доступные части файла, 
    перезаписав блоки, которые не читаются нулями, так что по крайней мере файл может быть прочитан и скопирован, 
    даже если  не все части файла верны. Многие мультимедийные приложения могут заполнить эти пробелы.

‎Все это делается в автоматическом режиме, поэтому ваши семейные фото и личные видео максимально защищены.

‎После завершения операции отображается журнал, который также сохраняется в папке «Документы» для дальнейшего использования.

‎Нет необходимости в установке. Вы можете извлечь архив во вложенную папку диска, содержащую 
    фотографии и запускайте его оттуда. Windows возможно предложит вам установить 
    .NET-Framework — это все, что вам нужно.

‎Если вы решите скопировать из первого каталога во второй, приложение будет рассматривать 
    первый каталог как первичный источник данных и второй каталог в качестве резервного. 
    Если приложение обнаруживает, что файл из первого содержит поврежденные блоки, оно все ещё может 
    попытаться восстановить блоки из резервной копии, либо восстановить старую версию
    того же файла из резервной копии во второй директории. Вам необходимо указать, что первая 
    директория недоступна для записи, чтобы приложение не пыталось изменить файлы в первом каталоге.

‎Приложение обычно работает в режиме синхронизации, а значит, будет пытаться скопировать 
    самую последнюю версию файлов/фотографий в другую соответствующую папку или на другой диск. Если вы удалите параметр
    синхронизации, приложение также может перезаписать новые файлы, находящиеся во втором каталоге, более старыми файлами,
    находящимися в первой директории.

‎Если указать один и тот же каталог или диск в первом и втором каталоге (например, E:\ и E:\ ), 
    приложение работает в том же режиме, что и SaveMyFiles — создает дополнительные данные, если они отсутствуют, 
    или проверяет и восстанавливает файлы, если были указаны соответствующие параметры.

> [!NOTE]
> ‎Если во второй директории находится специальный файл, имя которого указано ниже [^1], 
>     Приложение не удаляет в нём файлы, даже когда вы указываете необходимость удаления файлов во втором каталоге.
  
[Вам нужна поддержка?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Вики](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Polski
<a name="pl"></a>
‎SyncFolders to aplikacja, która ma na celu pomóc w synchronizacji dwóch folderów lub dysków. 
    Przy ustawieniach domyślnych ta aplikacja tworzy ukryte pliki, około 1% rozmiaru oryginalnych plików, 
    które pozwalają na Całkowicie odzyskasz sprawność po awariach pojedynczych bloków. 
    Istnieją dwie warstwy ochrony:
1. Przechowujesz dwa różne dyski z pełnymi kopiami plików.
2. Nawet jeśli dysk stanie się niedostępny, np. awaria zasilania lub inne problemy, 
      awarie pojedynczych bloków A także większe zakresy błędów w plikach można przywrócić 
      za pomocą dodatkowo zapisanych informacji.

‎Oryginalne pliki i dodatkowo zapisane informacje mogą być weryfikowane przez aplikację.
    W przypadku błędów, aplikacja spróbuje naprawić plik. Jeśli istnieje drugi egzemplarz z 
    tą samą datą i tej samej długości, a następnie aplikacja spróbuje odzyskać pojedyncze 
    bloki z drugiej kopii. Jeśli to się nie powiedzie, aplikacja Będzie próbował odzyskać 
    pojedyncze bloki z dodatkowo zapisanych informacji z kopii zapasowej w ukrytych plikach.

‎W przypadku, gdy wszystkie wymienione środki zawiodą, aplikacja spróbuje również 
    przywrócić starszą kopię z innej Katalog. Jest to standardowe zachowanie aplikacji 
    do tworzenia kopii zapasowych: przywracają stare kopie tych samych plików.

‎W końcu, jeśli nic nie zadziałało, aplikacja odzyska dostępne części pliku poprzez 
    nadpisanie nieczytelne bloki z zerami, więc przynajmniej plik może być odczytany i skopiowany, 
    nawet jeśli nie wszystkie jego części są w porządku. Wiele aplikacji multimedialnych 
    może przeskakiwać te brakujące części.

‎Wszystko to odbywa się automatycznie, dzięki czemu Twoje osobiste zdjęcia rodzinne i filmy są jak najlepiej bezpieczne.

‎Dziennik jest wyświetlany po zakończeniu operacji, a także zapisywany w folderze Dokumenty do późniejszego wykorzystania.

‎Nie ma potrzeby instalacji. Archiwum można rozpakować do podfolderu na dysku, 
    który zawiera zdjęcia i uruchom go stamtąd. System Windows w końcu poprosi o zainstalowanie 
    .NET-Framework, to wszystko, czego potrzebujesz.

‎Jeśli zdecydujesz się skopiować z pierwszego do drugiego katalogu, aplikacja będzie 
    traktować pierwszy katalog jako podstawowy źródło danych i drugi katalog jako kopia zapasowa. 
    Jeśli aplikacja wykryje, że plik w pierwszym katalogu zawiera Uszkodzone bloki, aplikacja nadal 
    może próbować przywrócić bloki z kopii zapasowej. Aplikacja może również przywrócić stare wersji 
    tego samego pliku z katalogu kopii zapasowej. Musisz określić, że pierwszy katalog nie jest zapisywalny, 
    Dzięki temu aplikacja nie próbuje modyfikować plików w pierwszym katalogu.

‎Aplikacja zwykle działa w trybie synchronizacji, co oznacza, że będzie próbowała skopiować 
    najnowszą wersję zdjęć do odpowiedniego innego folderu lub dysku. Jeśli usuniesz tryb synchronizacji, 
    aplikacja Może również nadpisać nowe pliki w drugim katalogu przez stare pliki w pierwszym katalogu.

‎Jeżeli w pierwszym i drugim katalogu podasz ten sam katalog lub dysk (np. E:\ i E:\ ), 
    aplikacja będzie działać w tym samym trybie co SaveMyFiles — utworzy dodatkowe dane, jeśli ich zabraknie, 
    lub zweryfikuje i naprawi pliki, jeśli określono odpowiednie opcje.

> [!NOTE]
> ‎Jeśli drugi katalog zawiera specjalny plik o nazwie podanej poniżej [^1], to 
>     aplikacja nie usuwa znajdujących się w nim plików. W takim przypadku ignoruje to, 
>     że określiłeś do usunięcia plików w drugim katalogu.
  
[Czy potrzebujesz wsparcia?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Вики](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Στα ελληνικά
<a name="gr"></a>
‎Το SyncFolders είναι μια εφαρμογή που έχει σχεδιαστεί για να σας βοηθήσει να συγχρονίσετε δύο φακέλους ή μονάδες δίσκου. 
    Με τις προεπιλεγμένες ρυθμίσεις, αυτή η εφαρμογή δημιουργεί κρυφά αρχεία, περίπου 1% του μεγέθους των αρχικών αρχείων, 
    τα οποία επιτρέπουν την πλήρη ανάκτηση από αποτυχίες μεμονωμένων μπλοκ δίσκων. Υπάρχουν δύο επίπεδα προστασίας:
1. Αποθηκεύετε δύο διαφορετικούς δίσκους με πλήρη αντίγραφα των αρχείων.
2. Ακόμα κι αν ένας δίσκος δεν είναι διαθέσιμος, για παράδειγμα λόγω διακοπής ρεύματος ή άλλων προβλημάτων, 
      μπορούν να ανακτηθούν μεμονωμένα μπλοκ αρχείων, καθώς και μεγαλύτερα εύρη σφαλμάτων αρχείων, 
      χρησιμοποιώντας πρόσθετες αποθηκευμένες πληροφορίες.

‎Τα αρχεία προέλευσης και οι αποθηκευμένες πρόσθετες πληροφορίες μπορούν να επαληθευτούν από την εφαρμογή.
    Εάν παρουσιαστούν σφάλματα, η εφαρμογή θα προσπαθήσει να επαναφέρει το αρχείο. Εάν υπάρχει μια δεύτερη παρουσία 
    ενός αρχείου με την ίδια ημερομηνία και το ίδιο μήκος, η εφαρμογή θα προσπαθήσει να εξαγάγει μεμονωμένα μπλοκ από το 
    άλλο αντίγραφο. Εάν αυτό αποτύχει, η εφαρμογή θα προσπαθήσει να συνδέσει πρόσθετες αποθηκευμένες πληροφορίες.

‎Εάν όλα τα παραπάνω αποτύχουν, η εφαρμογή θα προσπαθήσει επίσης να αντιγράψει το παλαιότερο 
    αντίγραφο σε άλλη μονάδα δίσκου, κάτι που αποτελεί τυπική συμπεριφορά για εφαρμογές δημιουργίας αντιγράφων ασφαλείας.

‎Τελικά, εάν τίποτα άλλο δεν λειτουργεί, η εφαρμογή θα ανακτήσει τα προσβάσιμα μέρη του 
    αρχείου αντικαθιστώντας τα μπλοκ που δεν είναι αναγνώσιμα με μηδενικά, έτσι ώστε τουλάχιστον το αρχείο να μπορεί 
    να διαβαστεί και να αντιγραφεί, ακόμα κι αν δεν είναι σωστά όλα τα μέρη του αρχείου. 
    Πολλές εφαρμογές πολυμέσων μπορούν να καλύψουν αυτά τα κενά.

‎Όλα αυτά γίνονται αυτόματα, ώστε οι οικογενειακές σας φωτογραφίες και τα προσωπικά σας βίντεο να προστατεύονται όσο το δυνατόν περισσότερο.

‎Μόλις ολοκληρωθεί η λειτουργία, εμφανίζεται ένα αρχείο καταγραφής και επίσης αποθηκεύεται στο φάκελο Documents για μελλοντική χρήση.

‎Δεν απαιτείται εγκατάσταση. Μπορείτε να εξαγάγετε το αρχείο zip σε έναν υποφάκελο στη 
    μονάδα δίσκου που περιέχει τις Φωτογραφίες και να το εκτελέσετε από εκεί. Τα Windows θα σας ζητήσουν επιτέλους 
    να εγκαταστήσετε το .NET Framework, αυτό είναι το μόνο που χρειάζεστε.

‎Εάν επιλέξετε να αντιγράψετε από τον πρώτο κατάλογο στον δεύτερο κατάλογο, 
    η εφαρμογή θα αντιμετωπίσει τον πρώτο κατάλογο ως την κύρια πηγή δεδομένων και τον δεύτερο κατάλογο ως αντίγραφο ασφαλείας. 
    Εάν η εφαρμογή εντοπίσει ότι ένα αρχείο στον πρώτο κατάλογο περιέχει εσφαλμένα μπλοκ, 
    μπορεί να προσπαθήσει να επαναφέρει τα μπλοκ από το αντίγραφο ασφαλείας στον δεύτερο φάκελο ή την 
    παλαιότερη έκδοση του ίδιου αρχείου από το αντίγραφο ασφαλείας του δεύτερου φακέλου. Πρέπει να καθορίσετε 
    ότι ο πρώτος κατάλογος δεν είναι εγγράψιμος, ώστε η εφαρμογή να μην επιχειρήσει να τροποποιήσει τα αρχεία στον πρώτο κατάλογο.

‎Η εφαρμογή συνήθως εκτελείται σε λειτουργία συγχρονισμού, πράγμα που σημαίνει ότι προσπαθεί να αντιγράψει 
    την πιο πρόσφατη έκδοση των φωτογραφιών ο ένας στον φάκελο ή τη μονάδα δίσκου του άλλου. Εάν απενεργοποιήσετε τη λειτουργία συγχρονισμού, 
    η εφαρμογή μπορεί επίσης να αντικαταστήσει νέα αρχεία στον δεύτερο κατάλογο με παλιά αρχεία στον πρώτο κατάλογο.

‎Εάν καθορίσετε τον ίδιο κατάλογο ή μονάδα δίσκου στον πρώτο και στον δεύτερο κατάλογο (π.χ. E:\ και E:\ ), 
    η εφαρμογή λειτουργεί στην ίδια λειτουργία με το SaveMyFiles - δημιουργεί πρόσθετα δεδομένα εάν λείπουν ή επαληθεύει 
    και επιδιορθώνει τα αρχεία εάν έχουν καθοριστεί οι κατάλληλες επιλογές.

> [!NOTE]
> ‎Εάν ο δεύτερος κατάλογος περιέχει ένα ειδικό αρχείο με το όνομα που δίνεται παρακάτω [^1], 
>     τότε η εφαρμογή δεν θα διαγράψει τα αρχεία σε αυτόν. Σε αυτήν την περίπτωση, 
>     αγνοεί ότι καθορίσατε ότι τα αρχεία στον δεύτερο κατάλογο πρέπει να διαγραφούν.
  
[Χρειάζεστε υποστήριξη;](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Nederlands
<a name="nl"></a>
‎SyncFolders is een applicatie die is ontworpen om u te helpen twee mappen of schijven gesynchroniseerd te houden. 
    Met standaardinstellingen maakt deze applicatie verborgen bestanden van ongeveer 1% van de grootte van de originele bestanden en kunt 
    u een volledig herstel uitvoeren na individuele blokfouten. Er zijn twee beschermingsniveaus:
1. U bewaart twee verschillende schijven met volledige kopieën van bestanden.
2. Zelfs als een schijf niet meer toegankelijk is, b.v. B. Door stroomuitval of andere problemen kunnen storingen 
      van individuele blokken en zelfs grotere foutgebieden in bestanden worden hersteld met extra opgeslagen informatie.

‎De originele bestanden en aanvullende opgeslagen informatie kunnen door de applicatie worden gecontroleerd. 
    In geval van fouten probeert de applicatie het bestand te herstellen. Als er een tweede kopie is met dezelfde datum en lengte, 
    probeert de app individuele blokken van de andere kopie te herstellen. Als dit niet lukt, probeert de app individuele blokken 
    te herstellen uit extra opgeslagen blokken met back-upinformatie in verborgen bestanden

‎Als alle bovenstaande maatregelen mislukken, zal de applicatie ook proberen een oudere kopie van 
    de andere versie vanaf de andere schijf te herstellen. Dit is het standaardgedrag van back-uptoepassingen.

‎Als niets werkte, herstelt de applicatie de beschikbare delen van het bestand door onleesbare 
    blokken met nullen te overschrijven, zodat het bestand in ieder geval gelezen en gekopieerd kan worden, 
    zelfs als niet alle delen ervan in orde zijn. Veel mediatoepassingen kunnen deze ontbrekende onderdelen overslaan.
    

‎Dit gebeurt allemaal automatisch, waardoor uw persoonlijke familiefoto's en -video's zo veilig mogelijk blijven.

‎Zodra het proces is voltooid, wordt een logboek weergegeven en ook opgeslagen in de map Documenten voor toekomstig gebruik.

‎Installatie is niet vereist. U kunt het zip-archief uitpakken naar een submap op de schijf die Foto's bevat en het 
    van daaruit uitvoeren. Windows zal u uiteindelijk vragen om .NET Framework, meer heb je niet nodig.

‎Als u ervoor kiest om van de eerste map naar de tweede map te kopiëren, behandelt de app de eerste map als de primaire 
    gegevensbron en de tweede map als de back-up. Als de toepassing detecteert dat een bestand in de eerste map slechte blokken bevat, 
    kan het nog steeds proberen de blokken uit de back-up in de tweede map of de oudere versie van hetzelfde bestand uit de back-up 
    in de tweede map te herstellen. U moet opgeven dat de eerste map niet beschrijfbaar is, zodat de toepassing niet probeert de 
    bestanden in de eerste map te wijzigen.

‎De applicatie werkt doorgaans in de synchronisatiemodus, wat betekent dat wordt geprobeerd de
    nieuwste versie van foto's naar elkaars map of schijf te kopiëren. Als u de synchronisatiemodus uitschakelt, 
    kan de toepassing ook nieuwe bestanden in de tweede map overschrijven met oude bestanden in de eerste map.

‎Als u in de eerste en tweede map dezelfde directory of hetzelfde station opgeeft (bijvoorbeeld E:\ en E:\ ), 
    werkt de toepassing in dezelfde modus als SaveMyFiles: er worden aanvullende gegevens gemaakt als deze ontbreken, 
    of de bestanden worden gecontroleerd en hersteld als er geschikte opties zijn opgegeven.

> [!NOTE]
> ‎Als de tweede map een speciaal bestand bevat met de onderstaande naam [^1], zal de applicatie de bestanden 
>     daarin niet verwijderen. In dit geval wordt genegeerd dat u hebt opgegeven dat de bestanden in de tweede map moeten worden verwijderd.
  
[Heeft u ondersteuning nodig?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Dansk
<a name="da"></a>
‎SyncFolders er et program designet til at hjælpe dig med at holde to mapper eller drev synkroniseret. 
    Med standardindstillinger opretter denne applikation skjulte filer omkring 1% af størrelsen af de originale filer og giver 
    dig mulighed for at udføre en fuld gendannelse efter individuelle blokeringsfejl. Der er to beskyttelsesniveauer:
1. Du beholder to forskellige drev med fulde kopier af filer.
2. Selvom et drev ikke længere er tilgængeligt, f.eks. B. på grund af strømsvigt eller andre problemer 
      kan fejl i enkelte blokke og endnu større fejlområder i filer gendannes med yderligere gemt information.

‎
      De originale filer og yderligere lagrede oplysninger kan kontrolleres af applikationen. I tilfælde af fejl
      forsøger applikationen at reparere filen. Hvis der er en anden kopi med samme dato og længde, forsøger appen at gendanne
      individuelle blokke fra den anden kopi.Hvis dette mislykkes, vil appen forsøge at gendanne individuelle blokke fra yderligere 
      sikkerhedskopieringsoplysninger gemt i skjulte filer.

‎Hvis alle ovenstående foranstaltninger mislykkes, vil applikationen også forsøge at gendanne en 
    ældre kopi af den anden fra det andet drev. Dette er standardadfærden for backup-applikationer.

‎Hvis intet virkede, gendanner applikationen de tilgængelige dele af filen ved at overskrive 
    ulæselige blokke med nuller, så i det mindste filen kan læses og kopieres, selvom ikke alle dele af den er i orden. 
    Mange medieapplikationer kan springe over disse manglende dele.

‎Alt dette sker automatisk, og holder dine personlige familiebilleder og videoer så sikre som muligt.

‎Når processen er fuldført, vil en log blive vist og også gemt i mappen Dokumenter til fremtidig reference.

‎Installation er ikke påkrævet. Du kan udpakke zip-arkivet til en undermappe på drevet, der indeholder Fotos, 
    og køre det derfra. Windows vil endelig bede dig om at installere .NET Framework, det er alt hvad du behøver.

‎Hvis du vælger at kopiere fra den første mappe til den anden mappe, vil appen behandle den første 
    mappe som den primære datakilde og den anden mappe som backup. Hvis programmet registrerer, at en fil i den første mappe 
    indeholder dårlige blokke, kan den stadig forsøge at gendanne blokkene fra sikkerhedskopien i den anden mappe eller den 
    ældre version af den samme fil fra sikkerhedskopien i den anden mappe. Du skal angive, at den første mappe ikke kan skrives, 
    så programmet ikke forsøger at ændre filerne i den første mappe.

‎Applikationen kører typisk i synkroniseringstilstand, hvilket betyder, at den forsøger at kopiere 
    den seneste version af billeder til hinandens mappe eller drev. Hvis du slår synkroniseringstilstand fra, 
    kan programmet også overskrive nye filer i den anden mappe med gamle filer i den første mappe.

‎Hvis du angiver den samme mappe eller det samme drev i den første og anden mappe (f.eks. E:\ og E:\ ), 
    fungerer programmet i samme tilstand som SaveMyFiles - det opretter yderligere data, hvis de mangler, 
    eller verificerer og reparerer filerne, hvis de relevante indstillinger er angivet.

> [!NOTE]
> ‎Hvis den anden mappe indeholder en speciel fil med navnet angivet nedenfor [^1], 
>     vil applikationen ikke slette filerne i den. I dette tilfælde ignorerer den, at du har angivet, at filerne i den anden mappe skal slettes.
  
[Har du brug for støtte?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Suomeksi
<a name="fi"></a>
‎SyncFolders on sovellus, joka on suunniteltu auttamaan sinua pitämään kaksi kansiota tai 
    asemaa synkronoituna. Oletusasetuksissa tämä sovellus luo piilotettuja tiedostoja noin 1 % alkuperäisten tiedostojen 
    koosta ja mahdollistaa täyden palautuksen yksittäisten estovirheiden jälkeen. Suojaustasoja on kaksi:
1. Säilytät kaksi eri asemaa täydellisillä tiedostokopioilla.
2. Vaikka asema ei ole enää käytettävissä, esim. B. sähkökatkon tai muiden ongelmien 
      vuoksi yksittäisten lohkojen vikoja ja vielä suurempiakin virhealueita tiedostoissa 
      voidaan palauttaa tallennetuilla lisätiedoilla.

‎Sovellus voi tarkistaa alkuperäiset tiedostot ja tallennetut lisätiedot. 
    Virheiden sattuessa sovellus yrittää korjata tiedoston. Jos on olemassa toinen kopio, 
    jolla on sama päivämäärä ja pituus, sovellus yrittää palauttaa yksittäisiä lohkoja toisesta kopiosta. 
    Jos tämä epäonnistuu, sovellus yrittää palauttaa yksittäisiä lohkoja piilotiedostoihin 
    tallennetuista lisävarmuuskopiotiedoista.

‎Jos kaikki yllä olevat toimenpiteet epäonnistuvat, sovellus yrittää myös palauttaa vanhemman 
    kopion toisesta asemasta. Tämä on varmuuskopiointisovellusten normaali toiminta.

‎Jos mikään ei auta, sovellus palauttaa tiedoston käytettävissä olevat osat kirjoittamalla 
    lukukelvottomien lohkojen päälle nollia, jotta ainakin tiedosto voidaan lukea ja kopioida, vaikka 
    kaikki sen osat eivät olisikaan kunnossa. Monet mediasovellukset voivat ohittaa nämä puuttuvat osat.

‎Kaikki tämä tapahtuu automaattisesti, mikä pitää henkilökohtaiset perhekuvasi ja videosi mahdollisimman turvassa.

‎Kun prosessi on valmis, loki tulee näkyviin ja tallennetaan myös Asiakirjat-kansioon myöhempää käyttöä varten.

‎Asennusta ei vaadita. Voit purkaa zip-arkiston valokuvat sisältävään aseman alikansioon ja suorittaa sen sieltä. 
    Windows pyytää lopulta asentamaan .NET Frameworkin, siinä kaikki mitä tarvitset.

‎Jos päätät kopioida ensimmäisestä hakemistosta toiseen, sovellus käsittelee ensimmäistä hakemistoa 
    ensisijaisena tietolähteenä ja toista hakemistoa varmuuskopiona. Jos sovellus havaitsee, että ensimmäisessä 
    hakemistossa oleva tiedosto sisältää virheellisiä lohkoja, se voi silti yrittää palauttaa lohkot toisen kansion 
    varmuuskopiosta tai saman tiedoston vanhemman version toisen hakemiston varmuuskopiosta. Sinun on määritettävä, 
    että ensimmäiseen hakemistoon ei voi kirjoittaa, jotta sovellus ei yritä muokata ensimmäisen hakemiston tiedostoja.

‎Sovellus toimii yleensä synkronointitilassa, mikä tarkoittaa, että se yrittää kopioida uusimman 
    version valokuvista toistensa kansioon tai asemaan. Jos poistat synkronointitilan käytöstä, sovellus voi myös korvata 
    uudet tiedostot toisessa hakemistossa vanhoilla tiedostoilla ensimmäisessä hakemistossa.

‎Jos määrität saman hakemiston tai aseman ensimmäiseen ja toiseen hakemistoon (esim. E:\ ja E:\ ), 
    sovellus toimii samassa tilassa kuin SaveMyFiles – se luo lisätietoja, jos niitä puuttuu, 
    tai tarkistaa ja korjaa tiedostot, jos asianmukaiset asetukset on määritetty.

> [!NOTE]
> ‎Jos toinen hakemisto sisältää erityisen tiedoston, jonka nimi on alla [^1], sovellus 
>     ei poista siinä olevia tiedostoja. Tässä tapauksessa se jättää huomioimatta, että määritit, että toisen hakemiston tiedostot pitäisi poistaa.
  
[Tarvitsetko tukea?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Svenska
<a name="sv"></a>
‎SyncFolders är ett program utformat för att hjälpa dig att hålla två mappar eller enheter synkroniserade.
      Med standardinställningar skapar denna applikation dolda filer cirka 1 % av storleken på originalfilerna och låter dig utföra
      en fullständig återställning efter individuella blockeringsfel. Det finns två skyddsnivåer:
1. Du behåller två olika enheter med fullständiga kopior av filer.
2. Även om en enhet inte längre är tillgänglig, t.ex. B. på grund av strömavbrott eller andra problem
        kan fel i enskilda block och ännu större felområden i filer återställas med ytterligare sparad information.

‎Originalfilerna och ytterligare lagrad information kan kontrolleras av applikationen. Vid fel försöker programmet reparera filen.
      Om det finns en andra kopia med samma datum och längd, försöker appen återställa enskilda block från den andra kopian.
      Om detta misslyckas kommer appen att försöka återställa enskilda block från ytterligare säkerhetskopieringsinformation lagrad i dolda filer.

‎Om alla ovanstående åtgärder misslyckas kommer programmet också att försöka återställa en äldre kopia av den andra från den andra enheten.
      Detta är standardbeteendet för säkerhetskopieringsapplikationer.

‎Om inget fungerade återställer applikationen de tillgängliga delarna av filen genom att skriva över
      oläsbara block med nollor så att åtminstone filen kan läsas och kopieras, även om inte alla delar av den är i ordning.
      Många medieapplikationer kan hoppa över dessa saknade delar.

‎Allt detta sker automatiskt, och håller dina personliga familjefoton och videor så säkra som möjligt.

‎När processen är klar kommer en logg att visas och även sparas i mappen Dokument för framtida referens.

‎Installation krävs inte. Du kan extrahera zip-arkivet till en undermapp på enheten som innehåller Foton
      och köra det därifrån. Windows kommer äntligen att be dig installera .NET Framework, det är allt du behöver.

‎Om du väljer att kopiera från den första katalogen till den andra katalogen kommer appen att behandla
      den första katalogen som primär datakälla och den andra katalogen som säkerhetskopia. Om programmet upptäcker att en fil i
      den första katalogen innehåller dåliga block, kan den fortfarande försöka återställa blocken från säkerhetskopian i den andra
      mappen eller den äldre versionen av samma fil från säkerhetskopian i den andra katalogen. Du måste ange att den första katalogen
      inte är skrivbar så att programmet inte försöker ändra filerna i den första katalogen.

‎Die Anwendung wird normalerweise im Synchronisationsmodus ausgeführt, was bedeutet, dass sie versucht,
      die neueste Version der Fotos zu kopieren in den jeweils anderen Ordner oder das jeweils andere Laufwerk. Wenn Sie den
      Synchronisationsmodus ausschalten, kann die Anwendung auch neue Dateien im zweiten Verzeichnis durch alte Dateien im
      ersten Verzeichnis überschreiben.

‎Om du anger samma katalog eller enhet i den första och andra katalogen (t.ex. E:\ och E:\ ), 
    fungerar programmet i samma läge som SaveMyFiles – det skapar ytterligare data om den saknas, 
    eller verifierar och reparerar filerna om lämpliga alternativ har angetts.

> [!NOTE]
> ‎Om den andra katalogen innehåller en speciell fil med namnet nedan [^1],kommer programmet inte att radera filerna i den.
>       I det här fallet ignorerar den att du angav att filerna i den andra katalogen ska raderas.
  
[Behöver du stöd?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Wiki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# Türkçe
<a name="tr"></a>
‎SyncFolders, iki klasörü veya sürücüyü senkronize tutmanıza yardımcı olmak için tasarlanmış bir uygulamadır. 
    Varsayılan ayarlarla bu uygulama, orijinal dosyaların boyutunun yaklaşık %1'i kadar gizli dosyalar oluşturur ve bireysel 
    blok hatalarından sonra tam kurtarma gerçekleştirmenize olanak tanır. İki koruma düzeyi vardır:
1. Dosyaların tam kopyalarını içeren iki farklı sürücüyü saklıyorsunuz.
2. Bir sürücüye artık erişilemese bile, örn. B. elektrik kesintisi veya diğer sorunlar nedeniyle, 
      bireysel bloklardaki arızalar ve dosyalardaki daha büyük hata alanları, kaydedilen ek bilgilerle geri yüklenebilir.

‎Orijinal dosyalar ve saklanan ek bilgiler uygulama tarafından kontrol edilebilir. Hata durumunda uygulama dosyayı 
    onarmaya çalışır. Aynı tarih ve uzunlukta ikinci bir kopya varsa uygulama diğer kopyadaki blokları tek tek geri yüklemeye çalışır. 
    Bu başarısız olursa uygulama, gizli dosyalarda saklanan ek yedekleme bilgilerinden tek tek blokları geri yüklemeye çalışacaktır.

‎Yukarıdaki önlemlerin tümü başarısız olursa, uygulama aynı zamanda diğerinin eski bir kopyasını diğer 
    sürücüden geri yüklemeye çalışacaktır. Bu, yedekleme uygulamalarının standart davranışıdır.

‎Hiçbir şey işe yaramazsa uygulama, okunamayan blokların üzerine sıfırlar yazarak dosyanın mevcut bölümlerini geri yükler, böylece dosyanın tüm bölümleri sıralı olmasa bile en azından dosya okunabilir ve kopyalanabilir. Birçok medya uygulaması bu eksik kısımları atlayabilmektedir.

‎Tüm bunlar otomatik olarak gerçekleşir ve kişisel aile fotoğraflarınızı ve videolarınızı mümkün olduğunca güvende tutar.

‎İşlem tamamlandığında bir günlük görüntülenecek ve ileride başvurmak üzere Belgeler klasörüne kaydedilecektir.

‎Kurulum gerekli değildir. Zip arşivini sürücüdeki Fotoğraflar'ın bulunduğu bir alt klasöre çıkarabilir 
    ve oradan çalıştırabilirsiniz. Windows sonunda sizden .NET Framework'ü kurmanızı isteyecek, ihtiyacınız olan tek şey bu.

‎Birinci dizinden ikinci dizine kopyalamayı seçerseniz uygulama, ilk dizini birincil veri kaynağı, 
    ikinci dizini ise yedek olarak ele alır. Uygulama, birinci dizindeki bir dosyanın bozuk bloklar içerdiğini tespit ederse, 
    yine de blokları ikinci klasördeki yedekten veya aynı dosyanın eski sürümünü ikinci dizindeki yedekten geri yüklemeyi deneyebilir. 
    Uygulamanın ilk dizindeki dosyaları değiştirmeye çalışmaması için ilk dizinin yazılabilir olmadığını belirtmeniz gerekir.

‎Uygulama genellikle senkronizasyon modunda çalışır; bu, fotoğrafların en son sürümlerini birbirlerinin klasörüne veya 
    sürücüsüne kopyalamaya çalıştığı anlamına gelir. Senkronizasyon modunu kapatırsanız, uygulama aynı zamanda ikinci dizindeki 
    yeni dosyaların üzerine birinci dizindeki eski dosyaları da yazabilir.

‎Birinci ve ikinci dizinde aynı dizini veya sürücüyü belirtirseniz (örneğin E:\ ve E:\ ), 
    uygulama SaveMyFiles ile aynı modda çalışır; eksikse ek veri oluşturur 
    veya uygun seçenekler belirtildiyse dosyaları doğrular ve onarır.

> [!NOTE]
> ‎İkinci dizin aşağıda [^1] adı verilen özel bir dosya içeriyorsa, uygulama içindeki dosyaları silmez. 
>     Bu durumda, ikinci dizindeki dosyaların silinmesi gerektiğini belirttiğiniz dikkate alınmaz.
  
[Desteğe mi ihtiyacınız var?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[Viki](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# 中文文本
<a name="chs"></a>
‎SyncFolders 是一款旨在帮助您保持两个文件夹或驱动器同步的应用程序。使用默认设置，此应用程序会创建隐藏文件，
    其大小约为原始文件的 1%，这使您可以从单块故障中完全恢复。有两个级别的保护：
1. 您保留两个不同的驱动器，其中包含文件的完整副本。
2. 即使驱动器变得无法访问，例如电源故障或其他问题、单块故障以及文件中较大的错误范围可以使用附加保存的信息进行恢复。

‎应用程序可以验证原始文件和其他保存的信息。倘 的错误，应用程序将尝试修复文件。如果有第二个副本具有相同的日期和 
    相同的长度，则 app 将尝试从另一个副本中恢复单个块。如果此操作失败，则应用 将尝试从隐藏文件中额外保存的备份信息中恢复单个块。

‎如果上述所有操作均失败，应用程序还将尝试将旧副本复制到另一个驱动器，这是备份应用程序的标准行为。

‎毕竟，如果没有任何效果，那么应用程序将通过用零覆盖不可读的块来恢复文件的可用部分，
    因此至少可以读取和复制文件，即使文件的所有部分并非都正常。
    许多媒体应用程序可以跳过这些缺失的部分。

‎所有这些都是自动完成的，因此您的个人家庭照片和视频会尽可能妥善地保存。

‎操作完成后，将显示日志并保存在“文档”文件夹中以供将来使用。

‎无需安装。您可以将存档提取到驱动器的子文件夹中，该子文件夹包含 照片并从那里运行它。
    Windows 最终会提示您安装 .NET-Framework，这就是您所需要的。

‎如果您选择从第一个目录复制到第二个目录，则应用程序会将第一个目录视为主目录 数据源和第二个目录作为备份。
      如果应用程序发现第一个目录中的文件包含 坏块，应用程序仍然可以尝试从 备份 中恢复块。该应用程序还可以恢复旧的 版本。
      您需要指定第一个目录不可写，以便应用程序不会尝试更改第一个目录中的文件。

‎应用程序通常以同步模式运行，这意味着它将尝试复制最新版本 的照片复制到相应的其他文件夹或驱动器。
    如果删除同步模式，则应用程序 也可以用第一个目录中的旧文件覆盖第二个目录中的新文件。

‎如果您在第一个和第二个目录中指定相同的目录或驱动器（例如 E:\ 和 E:\ ），
    则该应用程序将以与 SaveMyFiles 相同的模式工作 - 如果丢失，它会创建额外的数据，
    或者如果指定了适当的选项，它会验证并修复文件。

> [!NOTE]
> ‎如果第二个目录包含特殊文件，其名称在 [^1] 下面指定，
>     则应用程序 不会删除其中的文件。在这种情况下，它会忽略您指定的删除第二个目录中的文件。
  
[您需要支持吗？](https://github.com/NataljaNeumann/SyncFolders/issues)  
[维基百科](https://github.com/NataljaNeumann/SyncFolders/wiki)  
‎
# 中文文字
<a name="cht"></a>
SyncFolders 是一款旨在帮助您同步两个文件夹或驱动器的应用程序。使用默认设置功能，
    该应用程序创建隐藏文件，其大小约为原始文件的 1%，从而允许从固体故障中完全恢复。有两个级别的保护：
1. 您保留兩個不同的驅動器，其中包含檔的完整副本。
2. 即使驅動器變得無法訪問，例如電源故障或其他問題、單個數據塊故障 此外，
      還可以使用額外保存的信息來恢復檔中更大的錯誤範圍。

‎應用程式可以驗證原始檔和其他儲存的資訊。倘 的錯誤，應用程式將嘗試修復檔。
    如果有第二個副本具有相同的日期和 相同的長度，則app將嘗試從另一個副本中恢復單個塊。
    如果此操作失敗，則應用 將嘗試從隱藏檔中額外保存的備份資訊中恢復單個塊。

‎如果上述所有操作都失敗，應用程式還將嘗試將舊副本複製到另一個驅動器，這是備份應用程式的標準行為。

‎畢竟，如果沒有任何效果，那麼應用程式將透過用零覆蓋不可讀的區塊來恢復文件的可用部分，
    因此至少可以讀取和複製文件，即使文件的所有部分並非都正常。許多媒體應用程式可以跳過這些缺少的部分。

‎所有這些都是自動完成的，因此您的個人家庭照片和視頻會盡可能妥善地保存。

‎操作完成後，將顯示日誌並保存在「文件」資料夾中以供將來使用。

‎無需安裝。您可以將存檔提取到驅動器的子資料夾中，該子資料夾包含 照片並從那裡運行它。
    Windows 最終會提示您安裝 .NET-Framework，這就是您所需要的。

‎如果您選擇從第一個目錄複製到第二個目錄，則應用程式會將第一個目錄視為主目錄 數據源和第二個目錄作為備份。
    如果應用程式發現第一個目錄中的檔包含 壞塊，應用程式仍然可以嘗試從備份中恢復塊。該應用程式還可以恢復舊的 版本。
    您需要指定第一個目錄不可寫，以便應用程式不會嘗試變更第一個目錄中的檔案。

‎應用程式通常以同步模式運行，這意味著它將嘗試複製最新版本 的照片複製到相應的其他資料夾或驅動器。
    如果刪除同步模式，則應用程式 也可以用第一個目錄中的舊檔覆蓋第二個目錄中的新檔。

‎如果您在第一個和第二個目錄中指定相同的目錄或磁碟機（例如 E:\ 和 E:\ ），
    則該應用程式將以與 SaveMyFiles 相同的模式工作 - 如果遺失，它會建立額外的數據，
    或者如果指定了適當的選項，它會驗證並修復檔案。

> [!NOTE]
> ‎如果第二個目錄包含特殊檔，其名稱在 [^1] 下面指定，
>     則應用程式 不會刪除其中的檔。在這種情況下，它會忽略您指定的刪除第二個目錄中的檔。
  
[您需要支援嗎？](https://github.com/NataljaNeumann/SyncFolders/issues)  
[維基百科](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# 日本語
<a name="ja"></a>
‎SyncFoldersは、二つのフォルダまたはドライブを同期させることを目的としたアプリケーションです。
      デフォルト設定では、このアプリケーションは、元のファイルのサイズの約一%の隠しファイルを作成します。
      単一のブロック障害から完全に回復します。保護には 二 つの層があります:
1. ファイルの完全なコピーを持つ 二 つの異なるドライブを保持します。
2. 停電やその他の問題によりディスクが使用できなくなった場合でも、保存されている追加情報を使用して、
      ファイルの個々のブロックだけでなく、広範囲のファイル エラーも回復できます。

‎元のファイルと追加で保存された情報は、アプリケーションで確認することができます。
    万が一の場合 エラーの場合、アプリケーションはファイルの修復を試みます。
    同じ日付の 二 番目のコピーがある場合 同じ長さの場合、アプリは他のコピーから一つのブロックを回復しようとします。
    これが失敗した場合、アプリ 隠しファイルに追加で保存されたバックアップ情報から単一のブロックを回復しようとします。

‎上記のすべての対策が失敗した場合、アプリケーションは他のコピーから古いコピーの復元も試みます ディレクトリ。
    これはバックアップアプリケーションの標準的な動作で、同じファイルの古いコピーを復元します。

‎他に何も機能しない場合、アプリケーションは最終的にエラー ブロックをゼロで上書きすることにより、
    ファイルのアクセス可能な部分を復元します。これにより、ファイルのすべての部分が正しい場合でも、
    少なくともファイルの読み取りとコピーが可能になります。多くのマルチメディア アプリケーションは、
    これらのギャップを埋めることができます。

‎これらはすべて自動的に行われるため、個人の家族の写真やビデオは可能な限り安全に保管されます。

‎操作の完了後にログが表示され、後で参照できるようにドキュメントフォルダーにも保存されます。

‎インストールの必要はありません。アーカイブは、次のファイルを含むドライブのサブフォルダに抽出できます 写真を撮って、
    そこから実行します。Windows は最終的にインストールするように求めます.NET-Framework、それだけで十分です。

‎最初のディレクトリから 二 番目のディレクトリにコピーすることを選択した場合、
    アプリは最初のディレクトリをプライマリとして扱います バックアップとしてデータソースと 二 番目のディレクトリ。
    アプリケーションが、最初のディレクトリ内のファイルに次のものが含まれていることを検出した場合 不良ブロックの場合でも、
    アプリケーションはバックアップからブロックの復元を試みることができます。
    アプリケーションは古いものを復元することもできます バックアップディレクトリからの同じファイルのバージョン。
    最初のディレクトリが書き込み可能でないことを指定する必要があります。 
    そうすれば、アプリケーションは最初のディレクトリ内のファイルを変更しようとしなくなります。

‎
      通常、アプリケーションは同期モードで実行されるため、最新バージョンのコピーが試行されます
      それぞれの他のフォルダまたはドライブへの写真の。同期モードを削除すると、アプリケーションは また、
      二 番目のディレクトリにある新しいファイルを 一 番目のディレクトリにある古いファイルで上書きすることもできます。

‎最初のディレクトリと 2 番目のディレクトリに同じディレクトリまたはドライブ (例: E:\ と E:\ ) を指定した場合、
    アプリケーションは SaveMyFiles と同じモードで動作します。つまり、データが不足している場合は追加のデータを作成し、
    適切なオプションが指定されている場合はファイルを検証して修復します。

> [!NOTE]
> ‎二 番目のディレクトリに [^1] で指定した名前の特別なファイルが含まれている場合、
>     アプリケーションは その中のファイルは削除されません。この場合、
>     二 番目のディレクトリ内のファイルを削除するように指定したことは無視されます。
  
[サポートが必要ですか?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[ウィキ](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# 한국인
<a name="ko"></a>
‎SyncFolders는 두 개의 폴더 또는 드라이브를 동기화 상태로 유지하는 데 도움이 되는 응용 프로그램입니다. 
    기본 설정을 사용하면 이 응용 프로그램은 원본 파일 크기의 약 1%인 숨겨진 파일을 생성하여 다음을 허용합니다. 
    단일 블록 실패에서 완전히 복구할 수 있습니다. 보호에는 두 가지 계층이 있습니다.
1. 파일의 전체 복사본이 있는 두 개의 서로 다른 드라이브를 유지합니다.
2. 드라이브에 액세스할 수 없게 되더라도(예: 정전 또는 기타 문제, 단일 블록 고장) 
      또한 추가로 저장된 정보를 사용하여 파일의 더 큰 오류 범위를 복원할 수 있습니다.

‎원본 파일 및 추가로 저장된 정보는 응용 프로그램에서 확인할 수 있습니다. 
    경우에 따라 오류의 경우 응용 프로그램에서 파일 복구를 시도합니다. 
    날짜가 같고 두 번째 사본이 있고 동일한 길이이면 앱은 다른 복사본에서 단일 블록을 복구하려고 시도합니다. 
    이것이 실패하면 앱 숨겨진 파일에 추가로 저장된 백업 정보에서 단일 블록을 복구하려고 시도합니다.

‎언급 된 모든 조치가 실패하는 경우 응용 프로그램은 다른 복사본의 
    이전 복사본도 복원하려고 시도합니다 디렉토리. 이것은 백업 응용 프로그램의 표준 동작입니다 : 
    동일한 파일의 오래된 복사본을 복원합니다.

‎결국 다른 방법이 작동하지 않으면 응용 프로그램은 읽을 수 없는 블록을 
    공으로 덮어써 파일의 액세스 가능한 부분을 복구하므로 파일의 모든 부분이 정확하지 않더라도 최소한 파일을 읽고 복사할 수 있습니다. 
    많은 멀티미디어 애플리케이션이 이러한 격차를 메울 수 있습니다.

‎이 모든 것이 자동으로 수행되므로 개인 가족 사진과 비디오는 가능한 한 안전하게 보관됩니다.

‎작업 완료 후 로그가 표시되며 나중에 참조할 수 있도록 문서 폴더에도 저장됩니다.

‎설치가 필요하지 않습니다. 사진이 포함된 디스크의 하위 폴더에 아카이브를 추출하고 거기에서 실행할 수 있습니다. 
    Windows는 결국 .NET-Framework를 설치하라는 메시지를 표시합니다. 이것이 필요한 전부입니다.

‎첫 번째 디렉터리에서 두 번째 디렉터리로 복사하도록 선택하면 앱은 첫 번째 
    디렉터리를 기본 디렉터리로 처리합니다 데이터 소스 및 두 번째 디렉토리를 백업으로 사용합니다. 
    응용 프로그램이 첫 번째 파일에 잘못된 블록이 포함되어 있음을 감지하면 여전히 백업에서 블록을 
    복원하거나 두 번째 디렉터리의 백업에서 동일한 파일의 이전 버전을 복원하려고 시도할 수 있습니다. 
    애플리케이션이 첫 번째 디렉터리의 파일을 변경하려고 시도하지 않도록 첫 번째 디렉터리에 쓸 수 없도록 지정해야 합니다.

‎응용 프로그램은 일반적으로 동기화 모드에서 실행되며, 
    이는 최신 버전을 복사하려고 시도한다는 것을 의미합니다 사진을 각각의 다른 폴더 또는 드라이브에 넣습니다. 
    동기화 모드를 제거하면 응용 프로그램이 두 번째 디렉토리의 새 파일을 첫 번째 디렉토리의 이전 파일로 덮어쓸 수도 있습니다.

‎첫 번째와 두 번째 디렉토리에 같은 디렉토리나 드라이브를 지정하면(예: E:\ 및 E:\ ), 
    해당 애플리케이션은 SaveMyFiles와 같은 모드로 작동합니다. 즉, 데이터가 누락된 경우 추가 데이터를 생성하고, 
    적절한 옵션이 지정된 경우 파일을 검증하고 복구합니다.

> [!NOTE]
> ‎두 번째 디렉토리에 [^1] 아래에 지정된 이름의 특수 파일이 
>     포함되어 있으면 응용 프로그램은 그 안에 있는 파일을 삭제하지 않습니다. 
>     이 경우 두 번째 디렉토리의 파일을 삭제하도록 지정한 것을 무시합니다.
  
[지원이 필요합니까?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[위키](https://github.com/NataljaNeumann/SyncFolders/wiki)‎
# भारतीय में
<a name="hi"></a>
‎SyncFolders एक ऐसा एप्लिकेशन है जिसका उद्देश्य आपको दो फ़ोल्डर्स या ड्राइव को सिंक्रनाइज़ रखने में मदद करना है। 
    डिफ़ॉल्ट सेटिंग्स के साथ यह एप्लीकेशन छिपी हुई फाइलें बनाता है, मूल फाइलों के आकार का लगभग 1%, 
    जो अनुमति देता है आप एकल ब्लॉक विफलताओं से पूरी तरह से उबरने के लिए। सुरक्षा की दो परतें हैं:
1. आप फ़ाइलों की पूरी प्रतियों के साथ दो अलग-अलग ड्राइव रखते हैं।
2. यहां तक कि अगर कोई ड्राइव दुर्गम हो जाता है, जैसे बिजली की विफलता या अन्य समस्याएं, 
      एकल ब्लॉक विफलताएं और अतिरिक्त रूप से सहेजी गई जानकारी का उपयोग करके फ़ाइलों में बड़ी त्रुटि श्रेणियों को पुनर्स्थापित किया जा सकता है।

‎मूल फ़ाइलों और अतिरिक्त रूप से सहेजी गई जानकारी को एप्लिकेशन द्वारा सत्यापित किया जा सकता है। मामले में त्रुटियों में से अनुप्रयोग फ़ाइल को
      यदि समान दिनांक और लंबाई वाली दूसरी प्रति है, तो ऐप दूसरी प्रति से एक ब्लॉक को पुनर्स्थापित करने का प्रयास करता है।
      यदि यह विफल रहता है तो ऐप छिपी हुई फ़ाइलों में अतिरिक्त रूप से सहेजी गई बैकअप जानकारी से एकल ब्लॉक को पुनर्प्राप्त करने का प्रयास करेंगे।

‎यदि उपरोक्त सभी विफल हो जाते हैं, तो एप्लिकेशन पुरानी प्रतिलिपि को किसी अन्य ड्राइव पर कॉपी करने का भी प्रयास करेगा, जो बैकअप अनुप्रयोगों के लिए मानक व्यवहार है।

‎आखिरकार, अगर कुछ भी काम नहीं करता है, तो एप्लिकेशन फ़ाइल के उपलब्ध हिस्सों को ओवरराइट करके पुनर्प्राप्त करेगा शून्य के साथ अपठनीय ब्लॉक, 
    इसलिए कम से कम फ़ाइल को पढ़ा और कॉपी किया जा सकता है, भले ही इसके सभी भाग ठीक न हों। कई मीडिया एप्लिकेशन इन लापता भागों पर कूद सकते हैं।

‎यह सब स्वचालित रूप से किया जाता है, इसलिए आपके व्यक्तिगत पारिवारिक फ़ोटो और वीडियो यथासंभव सुरक्षित रखे जाते हैं।

‎ऑपरेशन के पूरा होने के बाद एक लॉग दिखाया जाता है और बाद में संदर्भ के लिए दस्तावेज़ फ़ोल्डर में भी सहेजा जाता है।

स्थापना की कोई आवश्यकता नहीं है। आप संग्रह को ड्राइव के सबफ़ोल्डर में निकाल सकते हैं जिसमें तस्वीरें 
    और इसे वहां से चलाएं। विंडोज अंततः आपको स्थापित करने के लिए संकेत देगा। नेट-फ्रेमवर्क, आपको बस इतना ही चाहिए।

‎यदि आप पहली से दूसरी निर्देशिका में कॉपी करना चुनते हैं, तो ऐप पहली निर्देशिका को प्राथमिक के रूप में मानेगा डेटा स्रोत 
    और बैकअप के रूप में दूसरी निर्देशिका। यदि एप्लिकेशन को पता चलता है कि पहली निर्देशिका में एक फ़ाइल है खराब ब्लॉक, 
    एप्लिकेशन अभी भी बैकअप से ब्लॉक को पुनर्स्थापित करने का प्रयास कर सकता है। एप्लिकेशन पुराने को भी पुनर्स्थापित कर सकता 
    है बैकअप निर्देशिका से एक ही फ़ाइल का संस्करण। आपको यह निर्दिष्ट करने की आवश्यकता है कि पहली निर्देशिका लिखने योग्य नहीं है, 
    इसलिए एप्लिकेशन पहली निर्देशिका में फ़ाइलों को संशोधित करने का प्रयास नहीं करता है।

‎एप्लिकेशन आमतौर पर सिंक्रनाइज़ेशन मोड में चलता है, जिसका अर्थ है कि यह नवीनतम संस्करण की प्रतिलिपि बनाने का प्रयास करेगा संबंधित 
    अन्य फ़ोल्डर या ड्राइव के लिए फोटो। यदि आप सिंक्रनाइज़ेशन मोड को हटाते हैं, तो एप्लिकेशन पहली निर्देशिका में पुरानी 
    फ़ाइलों द्वारा दूसरी निर्देशिका में नई फ़ाइलों को भी अधिलेखित कर सकते हैं।

‎यदि आप पहली और दूसरी निर्देशिका में एक ही निर्देशिका या ड्राइव निर्दिष्ट करते हैं (उदाहरण के लिए E:\ और E:\ ), 
    तो अनुप्रयोग SaveMyFiles के समान मोड में कार्य करता है - यदि डेटा गायब है तो यह अतिरिक्त डेटा बनाता है, 
    या यदि उपयुक्त विकल्प निर्दिष्ट किए गए हैं तो फ़ाइलों को सत्यापित और सुधारता है।

> [!NOTE]
> ‎यदि दूसरी निर्देशिका में विशेष फ़ाइल है, जिसका नाम नीचे निर्दिष्ट है [^1], 
>     तो एप्लिकेशन इसमें फ़ाइलों को नहीं हटाता है। इस स्थिति में, यह अनदेखा करता है कि आपने दूसरी निर्देशिका में फ़ाइलों को हटाने के लिए निर्दिष्ट किया है।
  
[क्या आपको समर्थन की आवश्यकता है?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[विकि](https://github.com/NataljaNeumann/SyncFolders/wiki)‏
# باللغة العربية
<a name="ar"></a>
‏SyncFolders هو تطبيق يهدف إلى مساعدتك في الحفاظ على مزامنة مجلدين أو محركات أقراص.
    مع الإعدادات الافتراضية ، يقوم هذا التثبيت بإنشاء ملفات مخفية ، حوالي 1٪ من حجم الملفات الأصلية ،
    والتي تسمح يمكنك التعافي تماما من فشل الكتلة الواحدة. هناك طبقتان من الحماية:
1. يمكنك الاحتفاظ بمحركي أقراص مختلفين مع نسخ كاملة من الملفات.
2. حتى إذا تعذر الوصول إلى محرك الأقراص ، على سبيل المثال انقطاع التيار الكهربائي أو مشاكل أخرى ،
      فشل الكتلة الواحدة وأيضا يمكن استعادة نطاقات الخطأ الأكبر في الملفات باستخدام المعلومات المحفوظة بشكل إضافي.

‏ يمكن التحقق من الملفات الأصلية والمعلومات المحفوظة بالإضافة إلى ذلك بواسطة التطبيق. في حالة من الأخطاء ،
   سيحاول التطبيق إصلاح الملف. إذا كانت هناك نسخة ثانية بنفس التاريخ و نفس الطول ،
   ثم سيحاول التطبيق استعادة كتل مفردة من النسخة الأخرى. إذا فشل هذا ،
   فالتطبيق سيحاول استرداد الكتل الفردية من معلومات النسخ الاحتياطي المحفوظة بشكل إضافي في الملفات المخفية.

‏ في حالة فشل جميع الإجراءات المذكورة ، سيحاول التطبيق أيضا استعادة نسخة قديمة من الأخرى دليل.
   هذا هو السلوك القياسي لتطبيقات النسخ الاحتياطي: فهي تستعيد النسخ القديمة من نفس الملفات.

‏شيء ، إذا لم ينجح شيء ، فسيستعيد التطبيق الأجزاء المتاحة من الملف عن طريق الكتابة فوقه كتل غير قابلة للقراءة مع أصفار ،
    لذلك على الأقل يمكن قراءة الملف ونسخه ، حتى لو لم تكن جميع أجزائه على ما يرام. يمكن للعديد من تطبيقات الوسائط القفز فوق هذه الأجزاء المفقودة.

‏ يتم كل هذا تلقائيا ، بحيث يتم الاحتفاظ بالصور ومقاطع الفيديو العائلية الشخصية الخاصة بك آمنة قدر الإمكان.

‏ يتم عرض سجل بعد الانتهاء من العملية ويتم حفظه أيضا في مجلد المستندات للرجوع إليه لاحقا.

‏ ليست هناك حاجة للتثبيت. يمكنك استخراج الأرشيف إلى مجلد فرعي لمحرك الأقراص يحتوي على الصور وتشغيلها من هناك. سيطالبك 
   Windows في النهاية بالتثبيت ‎.NET-Framework‏ ، هذا كل ما تحتاجه.

‏ إذا اخترت النسخ من الدليل الأول إلى الدليل الثاني ،
   فسيتعامل التطبيق مع الدليل الأول على أنه أساسي مصدر البيانات و
   الدليل الثاني كنسخة احتياطية. إذا اكتشف التطبيق أن ملفا في الدليل الأول يحتوي على الكتل السيئة ،
   لا يزال بإمكان التطبيق محاولة استعادة الكتل من النسخة الاحتياطية.
   يمكن للتطبيق أيضا استعادة القديم إصدار الملف نفسه من دليل النسخ الاحتياطي.
   تحتاج إلى تحديد أن الدليل الأول غير قابل للكتابة ،
   لذلك لا يحاول التطبيق تعديل الملفات الموجودة في الدليل الأول.

‏ عادة ما يعمل التطبيق في وضع المزامنة ، مما يعني أنه سيحاول نسخ أحدث إصدار من الصور إلى المجلد أو محرك الأقراص الآخر المعني.
   إذا قمت بإزالة وضع المزامنة ، تطبيق التطبيق يمكن أيضا استبدال الملفات الجديدة في الدليل الثاني بالملفات القديمة في الدليل الأول.

‏إذا قمت بتحديد نفس الدليل أو محرك الأقراص في الدليل الأول والثاني (على سبيل المثال E:\ و E:\ )، 
      يعمل التطبيق بنفس الوضع مثل SaveMyFiles - فهو ينشئ بيانات إضافية إذا كانت مفقودة،
      أو يتحقق من الملفات ويصلحها إذا تم تحديد الخيارات المناسبة.

> [!NOTE]
> ‏ إذا كان الدليل الثاني يحتوي على ملف خاص ، بالاسم المحدد أدناه [^1] ،
>    فإن التطبيق لا يحذف الملفات الموجودة فيه. في هذه الحالة، يتجاهل ما حددته لحذف الملفات الموجودة في الدليل الثاني.

‏[هل تحتاج إلى دعم؟](https://github.com/NataljaNeumann/SyncFolders/issues)  
[ويكي](https://github.com/NataljaNeumann/SyncFolders/wiki)
‏
# עִברִית
<a name="he"></a>
‏ היא אפליקציה שנועדה לעזור לך לשמור שתי תיקיות או כוננים מסונכרנים. עם הגדרות ברירת המחדל,
    יישום זה יוצר קבצים נסתרים בקירוב 1% מגודל הקבצים המקוריים, ומאפשר לך לבצע שחזור מלא מתקלות בלוק בודד. ישנן שתי רמות הגנה:
1. אתה שומר שני כוננים שונים עם עותקים מלאים של קבצים.
2. גם אם כונן הופך לבלתי נגיש, למשל הפסקת חשמל או בעיות אחרות,
      כשלים בבלוק יחיד וגם טווחי שגיאה גדולים יותר בקבצים ניתן לשחזר באמצעות מידע שנשמר בנוסף.

‏הקבצים המקוריים והמידע שנשמר בנוסף יכולים להיות מאומתים על ידי היישום. במקרה של שגיאות היישום ינסה לתקן את הקובץ.
    אם יש עותק שני עם אותו תאריך וכן באותו אורך, ואז האפליקציה תנסה לשחזר בלוקים בודדים מהעותק השני.
    אם פעולה זו נכשלת, האפליקציה ינסה לשחזר בלוקים בודדים ממידע גיבוי שנשמר בנוסף בקבצים מוסתרים.

‏  במקרה שכל האמצעים שהוזכרו נכשלים, היישום ינסה גם לשחזר עותק ישן יותר מהשני מדריך.
    זוהי ההתנהגות הסטנדרטית של יישומי גיבוי: הם משחזרים עותקים ישנים של אותם קבצים.

‏ אחרי הכל, אם שום דבר לא עבד, אז היישום ישחזר את החלקים הזמינים של הקובץ על
    ידי החלפת בלוקים בלתי קריאים עם אפסים, כך שלפחות ניתן לקרוא ולהעתיק את הקובץ,
    גם אם לא כל חלקיו בסדר. יישומי מדיה רבים יכולים לדלג מעל חלקים חסרים אלה.

‏ כל זה נעשה באופן אוטומטי, כך שהתמונות והסרטונים המשפחתיים האישיים שלך נשמרים בטוחים ככל האפשר.

‏ יומן מוצג לאחר השלמת הפעולה ונשמר גם בתיקיה 'מסמכים' לעיון במועד מאוחר יותר.

‏ אין צורך בהתקנה. באפשרותך לחלץ את הארכיון לתוך תיקיית משנה של הכונן המכיל תמונות ולהפעיל אותו משם. 
    Windows יבקש ממך בסופו של דבר להתקין ‎.NET-Framework‏, זה כל מה שאתה צריך.

‏ אם תבחר להעתיק מהספריה הראשונה לשנייה, האפליקציה תתייחס לספריה הראשונה כראשית מקור נתונים וספריה שנייה כגיבוי.
    אם היישום מגלה שקובץ בספריה הראשונה מכיל בלוקים רעים, היישום עדיין יכול לנסות לשחזר את הבלוקים מגיבוי.
    היישום יכול גם לשחזר ישן גרסה של אותו קובץ מספריית הגיבוי.
    עליך לציין כי הספרייה הראשונה אינה ניתנת לכתיבה, כך שהיישום אינו מנסה לשנות את הקבצים בספריה הראשונה.

‏ היישום פועל בדרך כלל במצב סינכרון, מה שאומר שהוא ינסה להעתיק את הגרסה החדשה ביותר של התמונות לתיקייה או לכונן המתאימים האחרים.
    אם תסיר את מצב הסנכרון, היישום יכול גם להחליף קבצים חדשים בספרייה השנייה על ידי קבצים ישנים בספרייה הראשונה.

‏אם תציין את אותה ספרייה או כונן בספרייה הראשונה והשנייה (לדוגמה, E:\ ו- E:\ ), 
      היישום יפעל באותו מצב כמו SaveMyFiles - הוא יוצר נתונים נוספים אם הם חסרים,
      או מאמת ומתקן את הקבצים אם צוינו אפשרויות מתאימות.

> [!NOTE]
> ‏ אם הספרייה השניה מכילה קובץ מיוחד, עם השם שצוין להלן [^1],
>     היישום אינו מוחק את הקבצים שבו. במקרה זה, הוא מתעלם מכך שציינת למחוק את הקבצים בספריה השניה.

‏[האם אתה צריך תמיכה?](https://github.com/NataljaNeumann/SyncFolders/issues)  
[ויקי](https://github.com/NataljaNeumann/SyncFolders/wiki)
  
  
  
  
[^1]: SyncFolders-Don't-Delete.txt
    

using Luma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Luma.Infrastructure.Persistence;

public sealed class DemoSeeder
{
    private readonly LumaDbContext _dbContext;
    private readonly ILogger<DemoSeeder> _logger;

    public const string DemoEmail = "demo@luma.app";
    public const string DemoName = "Elif Aşık";
    public const string DemoDreamTitle = "Berlin'de Sürdürülebilir Tasarım Stüdyosu Kurmak";

    public DemoSeeder(LumaDbContext dbContext, ILogger<DemoSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<User> EnsureDemoUserAndDataAsync(CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.Dreams)
            .FirstOrDefaultAsync(u => u.Email == DemoEmail, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = DemoEmail,
                Name = DemoName,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created demo user: {Email} ({UserId})", DemoEmail, user.Id);
        }

        var dream = await _dbContext.Dreams
            .Include(d => d.FutureProfile)
                .ThenInclude(fp => fp!.ThemeConfig)
            .Include(d => d.FutureScenario)
            .Include(d => d.Goals)
            .Include(d => d.DailyActions)
            .Include(d => d.Reflections)
            .Include(d => d.DailyPlanSnapshots)
                .ThenInclude(s => s.Actions)
            .FirstOrDefaultAsync(d => d.UserId == user.Id && d.Title == DemoDreamTitle, cancellationToken);

        if (dream is null)
        {
            dream = new Dream
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Title = DemoDreamTitle,
                Description = "Berlin Kreuzberg'de döngüsel ekonomi, biyo-materyaller ve yerel zanaatkarlarla çalışan, yarının estetiğini bugünden kuran sürdürülebilir bir tasarım stüdyosu.",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };
            _dbContext.Dreams.Add(dream);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created demo dream: {Title} ({DreamId})", dream.Title, dream.Id);
        }

        // 1. FutureProfile & ThemeConfig
        if (dream.FutureProfile is null)
        {
            var profile = new FutureProfile
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                Identity = "Sürdürülebilir Tasarımcı & Kurucu",
                Environment = "city",
                Lifestyle = new List<string>
                {
                    "Sabah eskiz ve malzeme seansları",
                    "Kreuzberg yerel atölye iş birlikleri",
                    "Biyo-kompozit malzeme Ar-Ge",
                    "Spree kıyısında bisikletli ulaşım"
                },
                Aspirations = new List<string>
                {
                    "Döngüsel tasarımda öncü bir ses olmak",
                    "Berlin Design Week sergisi açmak",
                    "Genç tasarımcılara sürdürülebilir üretim mentorluğu vermek"
                },
                Values = new List<string>
                {
                    "Sürdürülebilirlik",
                    "Zanaat & Özgünlük",
                    "Topluluk",
                    "Dingin Üretim"
                },
                Mood = "hopeful",
                Pace = "active",
                Summary = "Berlin'in yaratıcı ekosisteminde, doğaya saygılı materyallerle çalışan ve geleceğin tasarım dilini şekillendiren bağımsız bir stüdyo hayatı.",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };

            var themeConfig = new ThemeConfig
            {
                Id = Guid.NewGuid(),
                FutureProfileId = profile.Id,
                Palette = "forest_mist",
                Atmosphere = "calm",
                Illustration = "creative_dreamer",
                Motion = "soft",
                Imagery = new List<ThemeImagery>
                {
                    ThemeImagery.City,
                    ThemeImagery.Workspace,
                    ThemeImagery.Nature
                },
                CoreAnchorKeys = new List<string> { "creativity", "nature", "purpose" },
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };

            profile.ThemeConfig = themeConfig;
            _dbContext.FutureProfiles.Add(profile);
            await _dbContext.SaveChangesAsync(cancellationToken);
            dream.FutureProfile = profile;
        }

        // 2. FutureScenario
        if (dream.FutureScenario is null)
        {
            var scenario = new FutureScenario
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                Title = "Spree Kıyısında Bir Sabah: Tasarım ve Dinginlik",
                Intro = "Kreuzberg'deki eski fabrika binasından dönüştürülmüş stüdyonda sabah güneşi masana vuruyor. Yanında sıcak yulaf sütlü kahven ve masada mantar meşesi ile deniz yosunundan ürettiğin yeni aydınlatma prototipleri.",
                Morning = "Sabah 07:30'da kalkış, Spree boyunca bisiklet sürüşü, stüdyoda 30 dakika dikkat dağıtıcısız eskiz ve malzeme incelemesi.",
                Daytime = "Öğleden sonra yerel marangoz Hans ile geri dönüştürülmüş ahşap birleşim detaylarını çözerken duyduğun derin tatmin.",
                Evening = "Günün sonunda stüdyoyu toparlayıp bahçede dostlarla bir araya gelerek günün küçük zaferlerini kutlamak.",
                EmotionalTone = "Huzurlu, amaç sahibi, yaratıcı ve üretken.",
                ClosingReflection = "Büyük hayaller, her gün stüdyoda atılan küçük ve sabırlı adımlarla somutlaşır.",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };
            _dbContext.FutureScenarios.Add(scenario);
            await _dbContext.SaveChangesAsync(cancellationToken);
            dream.FutureScenario = scenario;
        }

        // 3. Goals
        var goals = await _dbContext.Goals
            .Where(g => g.DreamId == dream.Id)
            .OrderBy(g => g.Order)
            .ToListAsync(cancellationToken);

        if (goals.Count == 0)
        {
            var goal1 = new Goal
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                Title = "Malzeme Araştırması ve Biyo-kompozit Ar-Ge",
                Description = "Yerel kaynaklı biyo-bozunur malzeme kütüphanesi oluşturmak ve prototip testlerini tamamlamak.",
                Category = "Career",
                WhyItMatters = "Sürdürülebilir ürünlerin temeli doğaya zarar vermeyen, döngüsel malzemelerden geçer.",
                CurrentState = "Literatür taraması yapıldı, ilk numuneler temin edildi.",
                Gap = "Sertleşme ve esneklik dengesini sağlayan ideal kompozit formülü henüz netleşmedi.",
                DesiredOutcome = "Stüdyonun ilk koleksiyonunda kullanılacak en az 2 onaylı biyo-kompozit malzeme formülü.",
                Order = 1,
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };

            var goal2 = new Goal
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                Title = "Berlin Tasarım Ekosistemi ve Zanaatkar Ağı",
                Description = "Neukölln ve Kreuzberg'deki geleneksel seramik ve ahşap ustalarıyla ortak üretim protokolleri kurmak.",
                Category = "Networking",
                WhyItMatters = "Yerel ustalık bilgisi, dijital tasarımı zamansız ve kaliteli nesnelere dönüştürür.",
                CurrentState = "Bölgedeki 4 usta ile ön temas kuruldu.",
                Gap = "Ortak prototip üretim takvimi ve malzeme tedarik anlaşmalarının resmiyete dökülmesi gerekiyor.",
                DesiredOutcome = "En az 2 yerel zanaatkar atölyesiyle devam eden aktif ortak üretim süreci.",
                Order = 2,
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };

            var goal3 = new Goal
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                Title = "İlk Koleksiyon Lansmanı ve Sergi",
                Description = "Berlin Design Week kapsamında 'Yeniden Döngü' serisini sergilemek.",
                Category = "Milestone",
                WhyItMatters = "Stüdyonun felsefesini ve tasarımlarını uluslararası tasarım topluluğuyla buluşturur.",
                CurrentState = "Konsept fikirler ve ilk 3 prototip eskizi hazır.",
                Gap = "Sergi stand tasarımı, basılı katalog ve dijital anlatı materyalleri tamamlanmalı.",
                DesiredOutcome = "Berlin Design Week kapsamında 100+ ziyaretçi ve basın yerleşimi.",
                Order = 3,
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };

            goals = new List<Goal> { goal1, goal2, goal3 };
            _dbContext.Goals.AddRange(goals);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 4. DailyActions (Today's Actions)
        var dailyActions = await _dbContext.DailyActions
            .Where(a => a.DreamId == dream.Id)
            .OrderBy(a => a.Order)
            .ToListAsync(cancellationToken);

        if (dailyActions.Count == 0)
        {
            var action1 = new DailyAction
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                GoalId = goals[0].Id,
                GoalTitle = goals[0].Title,
                Title = "Mantar Meşesi & Biyo-Reçine Numunelerini İncele",
                Description = "Dün gelen kompozit numunelerinin esneklik ve sertleşme testlerini stüdyo masasında kontrol et.",
                TimeOfDay = "Morning",
                EstimatedMinutes = 25,
                Intention = "Malzemeye dokunarak güne odaklanmış ve merak dolu bir başlangıç yapmak.",
                Order = 1,
                Status = "Completed",
                CompletedAt = DateTime.UtcNow.Date.AddHours(9).AddMinutes(30)
            };

            var action2 = new DailyAction
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                GoalId = goals[1].Id,
                GoalTitle = goals[1].Title,
                Title = "Marangoz Hans ile Çerçeve Birleşim Detaylarını Görüş",
                Description = "Yeni konsol tasarımı için ahşap zıvana detaylarını çizim üzerinden netleştir.",
                TimeOfDay = "Afternoon",
                EstimatedMinutes = 45,
                Intention = "Zanaatkarın ustalığına saygı duyarak ortak bir çözüm üretmek.",
                Order = 2,
                Status = "Pending",
                CompletedAt = null
            };

            var action3 = new DailyAction
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                GoalId = goals[2].Id,
                GoalTitle = goals[2].Title,
                Title = "Günün Tasarım Notlarını ve Stüdyo Günlüğünü Kaydet",
                Description = "Bugün öğrenilen malzeme püf noktalarını ve bir sonraki adım için fikirleri günlüğe aktar.",
                TimeOfDay = "Evening",
                EstimatedMinutes = 15,
                Intention = "Günü dingin bir zihin ve minnetle tamamlamak.",
                Order = 3,
                Status = "Pending",
                CompletedAt = null
            };

            dailyActions = new List<DailyAction> { action1, action2, action3 };
            _dbContext.DailyActions.AddRange(dailyActions);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 5. DailyPlanSnapshots & Reflections for past 5-6 days
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingSnapshots = await _dbContext.DailyPlanSnapshots
            .Where(s => s.DreamId == dream.Id)
            .ToDictionaryAsync(s => s.PlanDate, cancellationToken);

        // Day 0: Today
        if (!existingSnapshots.ContainsKey(today))
        {
            var todaySnapshot = new DailyPlanSnapshot
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                PlanDate = today,
                GeneratedAt = DateTime.UtcNow.Date.AddHours(8),
                Actions = dailyActions.Select(a => new DailyPlanActionSnapshot
                {
                    Id = Guid.NewGuid(),
                    SourceDailyActionId = a.Id,
                    GoalId = a.GoalId,
                    GoalTitle = a.GoalTitle,
                    Title = a.Title,
                    Description = a.Description,
                    TimeOfDay = a.TimeOfDay,
                    EstimatedMinutes = a.EstimatedMinutes,
                    Intention = a.Intention,
                    Order = a.Order,
                    Status = a.Status,
                    CompletedAt = a.CompletedAt
                }).ToList()
            };
            _dbContext.DailyPlanSnapshots.Add(todaySnapshot);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Day -1 (Yesterday)
        var dayMinus1 = today.AddDays(-1);
        if (!existingSnapshots.ContainsKey(dayMinus1))
        {
            var snap1 = new DailyPlanSnapshot
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                PlanDate = dayMinus1,
                GeneratedAt = DateTime.UtcNow.AddDays(-1).Date.AddHours(8),
                Actions = new List<DailyPlanActionSnapshot>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[0].Id,
                        GoalTitle = goals[0].Title,
                        Title = "Doğal pigment karışımlarını test et",
                        Description = "Toprak tonlarında 4 farklı organik pigmenti reçineye karıştırarak renk stabilitesini gözlemle.",
                        TimeOfDay = "Morning",
                        EstimatedMinutes = 30,
                        Intention = "Doğanın renklerine güvenmek.",
                        Order = 1,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-1).Date.AddHours(10)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[1].Id,
                        GoalTitle = goals[1].Title,
                        Title = "Hans'ın atölyesinde ilk ahşap birleşim numunesini al",
                        Description = "Geri dönüştürülmüş meşe numunesiyle ilk kenar pahını tamamla.",
                        TimeOfDay = "Afternoon",
                        EstimatedMinutes = 40,
                        Intention = "Ustanın tecrübesine kulak vermek.",
                        Order = 2,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-1).Date.AddHours(15)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[2].Id,
                        GoalTitle = goals[2].Title,
                        Title = "Koleksiyon eskizlerini dijital ortama aktar",
                        Description = "Defterdeki 3 boyutlu aydınlatma taslaklarını vektörel olarak tara.",
                        TimeOfDay = "Evening",
                        EstimatedMinutes = 20,
                        Intention = "Fikri netleştirmek.",
                        Order = 3,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-1).Date.AddHours(19)
                    }
                }
            };
            _dbContext.DailyPlanSnapshots.Add(snap1);

            var ref1 = new DreamReflection
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                DailyPlanSnapshotId = snap1.Id,
                Mood = "Umutlu & Dingin",
                Notes = "Hans ile ilk birleşim modelini tamamladık. Dokusu ve kokusu mükemmel oldu.",
                Insight = "Sabırla işlenen detaylar tasarıma ruh katıyor.",
                CreatedAt = DateTime.UtcNow.AddDays(-1).Date.AddHours(20)
            };
            _dbContext.Reflections.Add(ref1);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Day -2
        var dayMinus2 = today.AddDays(-2);
        if (!existingSnapshots.ContainsKey(dayMinus2))
        {
            var snap2 = new DailyPlanSnapshot
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                PlanDate = dayMinus2,
                GeneratedAt = DateTime.UtcNow.AddDays(-2).Date.AddHours(8),
                Actions = new List<DailyPlanActionSnapshot>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[0].Id,
                        GoalTitle = goals[0].Title,
                        Title = "Kreuzberg malzeme geri dönüşüm merkezini ziyaret et",
                        Description = "Kullanılmayan mimari numuneleri ayıkla ve stüdyoya taşı.",
                        TimeOfDay = "Morning",
                        EstimatedMinutes = 45,
                        Intention = "Atıkları değere dönüştürmek.",
                        Order = 1,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-2).Date.AddHours(11)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[1].Id,
                        GoalTitle = goals[1].Title,
                        Title = "Seramik atölyesi Anna ile kahve randevusu",
                        Description = "Toprak kaplar ile ahşap kaidelerin uyumunu konuş.",
                        TimeOfDay = "Afternoon",
                        EstimatedMinutes = 35,
                        Intention = "Samimi bir yaratıcı bağ kurmak.",
                        Order = 2,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-2).Date.AddHours(16)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[2].Id,
                        GoalTitle = goals[2].Title,
                        Title = "Gece stüdyo aydınlatma simülasyonu",
                        Description = "Farklı lümen değerleriyle gölge oyunlarını kaydet.",
                        TimeOfDay = "Evening",
                        EstimatedMinutes = 30,
                        Intention = "Işığın dinginliğini hissetmek.",
                        Order = 3,
                        Status = "Skipped",
                        CompletedAt = null
                    }
                }
            };
            _dbContext.DailyPlanSnapshots.Add(snap2);

            var ref2 = new DreamReflection
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                DailyPlanSnapshotId = snap2.Id,
                Mood = "Biraz yorgun ama kararlı",
                Notes = "Berlin yağmurluydu, stüdyoda malzeme testleriyle geçti. Akşam dinlenmeye alan açtım.",
                Insight = "Bazen bir adımı erteleyip dinlenmek, yarının adımlarını güçlendirir.",
                CreatedAt = DateTime.UtcNow.AddDays(-2).Date.AddHours(20)
            };
            _dbContext.Reflections.Add(ref2);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Day -3
        var dayMinus3 = today.AddDays(-3);
        if (!existingSnapshots.ContainsKey(dayMinus3))
        {
            var snap3 = new DailyPlanSnapshot
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                PlanDate = dayMinus3,
                GeneratedAt = DateTime.UtcNow.AddDays(-3).Date.AddHours(8),
                Actions = new List<DailyPlanActionSnapshot>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[0].Id,
                        GoalTitle = goals[0].Title,
                        Title = "Biyo-reçine sertleştirici oranlarını hesapla",
                        Description = "3 farklı karışım oranı için laboratuvar çizelgesi oluştur.",
                        TimeOfDay = "Morning",
                        EstimatedMinutes = 25,
                        Intention = "Titizlikle öğrenmek.",
                        Order = 1,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-3).Date.AddHours(10)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[2].Id,
                        GoalTitle = goals[2].Title,
                        Title = "Berlin Design Week açık çağrı başvuru metnini taslakla",
                        Description = "Koleksiyon manifestosunu 200 kelimede özetle.",
                        TimeOfDay = "Afternoon",
                        EstimatedMinutes = 40,
                        Intention = "Kelimelere cesaret katmak.",
                        Order = 2,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-3).Date.AddHours(14)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[1].Id,
                        GoalTitle = goals[1].Title,
                        Title = "Stüdyo alet çantasını ve çalışma tezgahını düzenle",
                        Description = "Yarınki üretim için tezgahı temizle.",
                        TimeOfDay = "Evening",
                        EstimatedMinutes = 15,
                        Intention = "Zihne ve alana ferahlık vermek.",
                        Order = 3,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-3).Date.AddHours(18)
                    }
                }
            };
            _dbContext.DailyPlanSnapshots.Add(snap3);

            var ref3 = new DreamReflection
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                DailyPlanSnapshotId = snap3.Id,
                Mood = "Yaratıcı akışta",
                Notes = "Yeni aydınlatma eskizleri bitti. Işık dağılımı tam istediğim yumuşaklıkta.",
                Insight = "Görselleştirdiğin her çizgi gerçeğe bir adım daha yakın.",
                CreatedAt = DateTime.UtcNow.AddDays(-3).Date.AddHours(20)
            };
            _dbContext.Reflections.Add(ref3);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Day -4
        var dayMinus4 = today.AddDays(-4);
        if (!existingSnapshots.ContainsKey(dayMinus4))
        {
            var snap4 = new DailyPlanSnapshot
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                PlanDate = dayMinus4,
                GeneratedAt = DateTime.UtcNow.AddDays(-4).Date.AddHours(8),
                Actions = new List<DailyPlanActionSnapshot>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[0].Id,
                        GoalTitle = goals[0].Title,
                        Title = "Yerel mantar üreticisiyle hammadde görüşmesi yap",
                        Description = "Hasat sonrası kalan miselyum bloklarının tedarik sürecini netleştir.",
                        TimeOfDay = "Morning",
                        EstimatedMinutes = 35,
                        Intention = "Döngüyü yerelden başlatmak.",
                        Order = 1,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-4).Date.AddHours(11)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[1].Id,
                        GoalTitle = goals[1].Title,
                        Title = "Stüdyo bütçe ve gider projeksiyonunu güncelle",
                        Description = "İlk 3 ayın malzeme maliyetlerini tabloya dök.",
                        TimeOfDay = "Afternoon",
                        EstimatedMinutes = 25,
                        Intention = "Geleceğe güvenle bakmak.",
                        Order = 2,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-4).Date.AddHours(15)
                    }
                }
            };
            _dbContext.DailyPlanSnapshots.Add(snap4);

            var ref4 = new DreamReflection
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                DailyPlanSnapshotId = snap4.Id,
                Mood = "Odaklanmış & Net",
                Notes = "Malzeme tedarikçisiyle ön protokol imzalandı. Üretim takvimi belirginleşti.",
                Insight = "Doğru ortaklarla çalışmak yolu hafifletiyor.",
                CreatedAt = DateTime.UtcNow.AddDays(-4).Date.AddHours(20)
            };
            _dbContext.Reflections.Add(ref4);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Day -5
        var dayMinus5 = today.AddDays(-5);
        if (!existingSnapshots.ContainsKey(dayMinus5))
        {
            var snap5 = new DailyPlanSnapshot
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                PlanDate = dayMinus5,
                GeneratedAt = DateTime.UtcNow.AddDays(-5).Date.AddHours(8),
                Actions = new List<DailyPlanActionSnapshot>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[1].Id,
                        GoalTitle = goals[1].Title,
                        Title = "Stüdyo anahtarlarını teslim al ve alanı havalandır",
                        Description = "Kreuzberg stüdyosuna ilk adımı at ve elektrik/su tesisatını kontrol et.",
                        TimeOfDay = "Morning",
                        EstimatedMinutes = 45,
                        Intention = "Bu eşikten şükranla geçmek.",
                        Order = 1,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-5).Date.AddHours(10)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[2].Id,
                        GoalTitle = goals[2].Title,
                        Title = "İlk stüdyo eskiz masasını yerleştir",
                        Description = "Güneşi en iyi alan pencerenin önüne masayı konumlandır.",
                        TimeOfDay = "Afternoon",
                        EstimatedMinutes = 30,
                        Intention = "Işığa doğru yerleşmek.",
                        Order = 2,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-5).Date.AddHours(14)
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        GoalId = goals[0].Id,
                        GoalTitle = goals[0].Title,
                        Title = "Stüdyo günlüğünün ilk sayfasını aç",
                        Description = "LUMA ile çıktığın yolculuğun niyetini ilk sayfaya yaz.",
                        TimeOfDay = "Evening",
                        EstimatedMinutes = 15,
                        Intention = "Büyük bir yolculuğun ilk nefesi.",
                        Order = 3,
                        Status = "Completed",
                        CompletedAt = DateTime.UtcNow.AddDays(-5).Date.AddHours(19)
                    }
                }
            };
            _dbContext.DailyPlanSnapshots.Add(snap5);

            var ref5 = new DreamReflection
            {
                Id = Guid.NewGuid(),
                DreamId = dream.Id,
                DailyPlanSnapshotId = snap5.Id,
                Mood = "Heyecanlı & Minnettar",
                Notes = "Stüdyo için resmi kira kontratı imzalandı ve anahtarları aldım!",
                Insight = "Hayal ettiğin mekan artık fiziksel bir gerçeklik.",
                CreatedAt = DateTime.UtcNow.AddDays(-5).Date.AddHours(20)
            };
            _dbContext.Reflections.Add(ref5);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Demo seed data verified and ready for User {UserId}, Dream {DreamId}", user.Id, dream.Id);
        return user;
    }
}
